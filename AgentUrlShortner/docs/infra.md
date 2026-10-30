# Infrastructure

## Tooling
- **Pulumi** (C#, `azure-native` provider), project `Infrastructure/` (`Pulumi.yaml` name: `CS-AgenticUrlShortner`).
- Entry: `Program.cs` → `Deployment.RunAsync<MyStack>()`.
- Config object `settings` bound to `Models/Settings`.

## Resources (`MyStack.cs`)
Naming prefix: `{stackName}-{settings.appName}`.
| Resource | Name | Notes |
|----------|------|-------|
| Resource Group | `{prefix}-rg` | |
| Log Analytics Workspace | `{prefix}-law` | SKU PerGB2018, retention `logRetentionInDays` |
| Application Insights | `{prefix}-ai` | Workspace-based |
| App Service Plan | `{prefix}-asp` | Linux, SKU `appServicePlanSku` |
| Web App | `{prefix}-api` | `DOTNETCORE|10.0`, HTTPS only, AlwaysOn, health `/health`, system-assigned identity |

Web App settings: `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`.

Outputs: `ResourceGroupName`, `WebAppHostName`.

## Stack config (`Pulumi.dev.yaml`)
```yaml
azure-native:location: centralus
CS-AgenticUrlShortner:settings:
  appName: agenticurlshortner
  aspnetCoreEnvironment: Development
  appServicePlanSku: B1
  logRetentionInDays: 30
```

## Commands
```powershell
cd Infrastructure
pulumi stack select dev
pulumi preview
pulumi up
```

## App configuration
| Key | Where | Purpose |
|-----|-------|---------|
| `AllowedOrigins` | appsettings / App Service setting | Comma-separated CORS origins |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Service setting | Telemetry |
| `ASPNETCORE_ENVIRONMENT` | App Service setting | Environment |

## Rules
- No secrets in code or YAML; use `pulumi config set --secret` or Key Vault + managed identity.
- New Azure resources go in `MyStack.cs` (or a new component class) and new settings in `Models/Settings` + every `Pulumi.<stack>.yaml`.
- Prefer managed identity over connection strings for data stores added later.
- Update this document when resources or settings change.
