namespace CS.AgenticUrlShortner.Infrastructure.Models
{
    internal sealed class Settings
    {
        public string AppName { get; set; } = "agenticurlshortner";

        public string AspnetCoreEnvironment { get; set; } = "Development";

        public string AppServicePlanSku { get; set; } = "B1";

        public int LogRetentionInDays { get; set; } = 30;
    }
}
