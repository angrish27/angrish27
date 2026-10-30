using CS.AgenticUrlShortner.Infrastructure.Models;
using Pulumi;
using Pulumi.AzureNative.ApplicationInsights;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

namespace CS.AgenticUrlShortner.Infrastructure
{
    internal sealed class MyStack : Stack
    {
        public MyStack()
        {
            var settings = new Config().RequireObject<Settings>("settings");
            var prefix = $"{Pulumi.Deployment.Instance.StackName}-{settings.AppName}";

            var resourceGroup = new ResourceGroup($"{prefix}-rg");

            var workspace = new Workspace($"{prefix}-law", new WorkspaceArgs
            {
                ResourceGroupName = resourceGroup.Name,
                RetentionInDays = settings.LogRetentionInDays,
                Sku = new Pulumi.AzureNative.OperationalInsights.Inputs.WorkspaceSkuArgs { Name = WorkspaceSkuNameEnum.PerGB2018 }
            });

            var appInsights = new Component($"{prefix}-ai", new ComponentArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Kind = "web",
                ApplicationType = ApplicationType.Web,
                WorkspaceResourceId = workspace.Id
            });

            var plan = new AppServicePlan($"{prefix}-asp", new AppServicePlanArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Kind = "linux",
                Reserved = true,
                Sku = new SkuDescriptionArgs { Name = settings.AppServicePlanSku }
            });

            var webApp = new WebApp($"{prefix}-api", new WebAppArgs
            {
                ResourceGroupName = resourceGroup.Name,
                ServerFarmId = plan.Id,
                HttpsOnly = true,
                Identity = new ManagedServiceIdentityArgs { Type = Pulumi.AzureNative.Web.ManagedServiceIdentityType.SystemAssigned },
                SiteConfig = new SiteConfigArgs
                {
                    LinuxFxVersion = "DOTNETCORE|10.0",
                    AlwaysOn = true,
                    HealthCheckPath = "/health",
                    AppSettings =
                    {
                        new NameValuePairArgs { Name = "ASPNETCORE_ENVIRONMENT", Value = settings.AspnetCoreEnvironment },
                        new NameValuePairArgs { Name = "APPLICATIONINSIGHTS_CONNECTION_STRING", Value = appInsights.ConnectionString }
                    }
                }
            });

            ResourceGroupName = resourceGroup.Name;
            WebAppHostName = webApp.DefaultHostName;
        }

        [Output]
        public Output<string> ResourceGroupName { get; set; } = null!;

        [Output]
        public Output<string> WebAppHostName { get; set; } = null!;
    }
}
