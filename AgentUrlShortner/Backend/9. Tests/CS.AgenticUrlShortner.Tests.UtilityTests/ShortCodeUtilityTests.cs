using System;
using CS.AgenticUrlShortner.Shared.Tests;
using CS.AgenticUrlShortner.Utilities.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.UtilityTests
{
    [TestClass]
    public class ShortCodeUtilityTests : TestBase
    {
        [TestMethod]
        [DataRow(0L, "AAAAAAAAAAA")]
        [DataRow(1L, "AAAAAAAAAAE")]
        public void ShortCodeUtility_EncodeEpochNanoseconds_ReturnsBase64Url(long value, string expected)
        {
            Assert.AreEqual(expected, UtilityFactory.Create<IShortCodeUtility>().EncodeUnixEpochNanoseconds(value));
        }

        [TestMethod]
        public void ShortCodeUtility_TimestampRoundTripsAtNanosecondUnitResolution()
        {
            var utility = UtilityFactory.Create<IShortCodeUtility>();
            var timestamp = DateTimeOffset.UtcNow;
            var epochNanoseconds = utility.ToUnixEpochNanoseconds(timestamp);
            var code = utility.EncodeUnixEpochNanoseconds(epochNanoseconds);

            Assert.IsTrue(utility.TryDecodeUnixEpochNanoseconds(code, out var decoded));
            Assert.AreEqual(epochNanoseconds, decoded);
            Assert.AreEqual((timestamp.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100, epochNanoseconds);
        }

        [TestMethod]
        [DataRow("AAAAAAAAAAE", true)]
        [DataRow("2tx", true)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("bad/code", false)]
        [DataRow("AAAAAAAAAA-", false)]
        public void ShortCodeUtility_IsValid_AcceptsCurrentAndLegacyCodes(string code, bool expected)
        {
            Assert.AreEqual(expected, UtilityFactory.Create<IShortCodeUtility>().IsValid(code));
        }

        [TestMethod]
        public void ShortCodeUtility_TryDecode_RejectsMalformedAndNegativeValues()
        {
            var utility = UtilityFactory.Create<IShortCodeUtility>();

            Assert.IsFalse(utility.TryDecodeUnixEpochNanoseconds("2tx", out _));
            Assert.IsFalse(utility.TryDecodeUnixEpochNanoseconds("__________8", out _));
        }
    }
}
