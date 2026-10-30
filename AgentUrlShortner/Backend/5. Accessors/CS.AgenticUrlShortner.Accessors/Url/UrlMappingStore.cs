using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common.Exceptions;
using CS.AgenticUrlShortner.Contracts.Logic;

namespace CS.AgenticUrlShortner.Accessors.Url
{
    internal sealed class UrlMappingStore : IUrlMappingStore
    {
        internal const long FirstSeqNumber = 11157;

        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly ConcurrentDictionary<string, UrlMapping> _byLongUrl = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, UrlMapping> _byCode = new(StringComparer.Ordinal);
        private readonly List<UrlMapping> _records = [];
        private string _filePath;
        private long _nextSeqNumber;
        private long _lastCodeEpochNanoseconds = -1;
        private volatile bool _loaded;

        public void Load(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (_loaded)
                throw new InvalidOperationException("The URL mapping store has already been loaded.");

            var records = File.Exists(filePath) ? ReadRecords(filePath) : [];

            foreach (var record in records)
            {
                _records.Add(record);
                _byCode[record.ShortCode] = record;
                _byLongUrl[record.ActualLongUrl] = record;
            }

            _nextSeqNumber = records.Count == 0 ? FirstSeqNumber : records.Max(r => r.SeqNumber) + 1;
            _lastCodeEpochNanoseconds = records
                .Where(record => record.CodeEpochNanoseconds.HasValue)
                .Select(record => record.CodeEpochNanoseconds.Value)
                .DefaultIfEmpty(-1)
                .Max();
            _filePath = filePath;
            _loaded = true;
        }

        public UrlMapping FindByLongUrl(string longUrl)
        {
            EnsureLoaded();
            return longUrl != null && _byLongUrl.TryGetValue(longUrl, out var mapping) ? mapping : null;
        }

        public UrlMapping FindByCode(string code)
        {
            EnsureLoaded();
            return code != null && _byCode.TryGetValue(code, out var mapping) ? mapping : null;
        }

        public async Task<(UrlMapping Mapping, bool Created)> GetOrAddAsync(string longUrl,
            long requestEpochNanoseconds, Func<long, string> encode, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(longUrl);
            ArgumentNullException.ThrowIfNull(encode);
            ArgumentOutOfRangeException.ThrowIfNegative(requestEpochNanoseconds);
            EnsureLoaded();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_byLongUrl.TryGetValue(longUrl, out var existing))
                    return (existing, false);

                var codeEpochNanoseconds = Math.Max(requestEpochNanoseconds, checked(_lastCodeEpochNanoseconds + 1));
                var code = encode(codeEpochNanoseconds);
                while (_byCode.ContainsKey(code))
                {
                    codeEpochNanoseconds = checked(codeEpochNanoseconds + 1);
                    code = encode(codeEpochNanoseconds);
                }

                var mapping = new UrlMapping
                {
                    SeqNumber = _nextSeqNumber,
                    ActualLongUrl = longUrl,
                    ShortCode = code,
                    CodeEpochNanoseconds = codeEpochNanoseconds
                };

                await PersistAsync([.. _records, mapping], cancellationToken).ConfigureAwait(false);

                _records.Add(mapping);
                _nextSeqNumber++;
                _byCode[mapping.ShortCode] = mapping;
                _byLongUrl[mapping.ActualLongUrl] = mapping;
                _lastCodeEpochNanoseconds = codeEpochNanoseconds;

                return (mapping, true);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void EnsureLoaded()
        {
            if (!_loaded)
                throw new InvalidOperationException("The URL mapping store has not been loaded.");
        }

        private async Task PersistAsync(List<UrlMapping> records, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_filePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _filePath + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, records, SerializerOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(true);
            }

            File.Move(tempPath, _filePath, true);
        }

        private static List<UrlMapping> ReadRecords(string filePath)
        {
            List<UrlMapping> records;
            try
            {
                records = JsonSerializer.Deserialize<List<UrlMapping>>(File.ReadAllText(filePath));
            }
            catch (JsonException ex)
            {
                throw Corrupt(filePath, "the content is not a valid JSON array of mappings", ex);
            }

            if (records == null)
                throw Corrupt(filePath, "the content is empty or null");

            var seqNumbers = new HashSet<long>();
            var longUrls = new HashSet<string>(StringComparer.Ordinal);
            var codes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var record in records)
            {
                if (record == null || record.SeqNumber < FirstSeqNumber
                    || string.IsNullOrEmpty(record.ActualLongUrl) || string.IsNullOrEmpty(record.ShortCode)
                    || record.CodeEpochNanoseconds < 0)
                    throw Corrupt(filePath, "a record is missing required fields or has invalid sequence or code timestamp data");

                if (!seqNumbers.Add(record.SeqNumber) || !longUrls.Add(record.ActualLongUrl) || !codes.Add(record.ShortCode))
                    throw Corrupt(filePath, $"duplicate data found for seqNumber {record.SeqNumber}");
            }

            return records;
        }

        private static AgenticUrlShortnerException Corrupt(string filePath, string reason, Exception inner = null)
        {
            var message = $"URL mapping store '{filePath}' is corrupt: {reason}. The file was left untouched.";
            return inner == null ? new AgenticUrlShortnerException(message) : new AgenticUrlShortnerException(message, inner);
        }
    }
}
