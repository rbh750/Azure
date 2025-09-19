using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Common.Resources.Enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Service.AppInsights;
using Service.Azure.Entra;
using Service.Azure.RetryPolicy;
using System.Text;

namespace Service.Azure.AppInsights;

// https://github.com/Azure/azure-sdk-for-net/blob/Azure.Monitor.Query_1.2.0/sdk/monitor/Azure.Monitor.Query/README.md
// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/monitor.query-readme?view=azure-dotnet
public class AppInsightsQueryService(
    IOptions<AppInsightsSettings> appInsightsSettings,
    IOptions<EntraSettings> entraSettings,
    IRetryPolicyService retryPolicyService) : IAppInsightsQueryService
{
    private readonly string clientId = entraSettings.Value.KeyVaultAuth!.ClientId!;
    private readonly string resourceGroupName = appInsightsSettings.Value.Resources!.ResourceGroupName!;
    private readonly string resourceNameApi = appInsightsSettings.Value.Resources!.ResourceNameApi!;
    private readonly string resourceNameWebJobs = appInsightsSettings.Value.Resources!.ResourceNameWebJobs!;
    private readonly string secret = entraSettings.Value.KeyVaultAuth!.Secret!;
    private readonly string subscriptionId = appInsightsSettings.Value.Resources!.SubscriptionId!;
    private readonly string tenantId = entraSettings.Value.KeyVaultAuth!.TenantId!;
    private readonly IRetryPolicyService retryPolicyService = retryPolicyService;
    private LogsQueryClient? client = null!;

    // Overloaded constructor for testing.
    public AppInsightsQueryService(
        IOptions<AppInsightsSettings> appInsightsSettings,
        IOptions<EntraSettings> entraSettings,
        IRetryPolicyService retryPolicyService,
        LogsQueryClient testClient)
        : this(appInsightsSettings, entraSettings, retryPolicyService)
    {
        client = testClient;
    }

    public void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        retryPolicyService.Configure(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    public async Task<JArray?> RunQuery(string query, AppInsightsResourceType appInsightsResourceType)
    {
        // Only create the client if the test client is not provided.
        client ??= new LogsQueryClient(new ClientSecretCredential(tenantId, clientId, secret));

        StringBuilder resourceId = new();
        resourceId.Append($"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/");

        switch (appInsightsResourceType)
        {
            case AppInsightsResourceType.Api:
                resourceId.Append($"{resourceNameApi}");
                break;
            case AppInsightsResourceType.Webjob:
                resourceId.Append($"{resourceNameWebJobs}");
                break;
            default:
                throw new NotImplementedException();
        }

        Response<LogsQueryResult>? results = null;

        await retryPolicyService.RunAsync(async () =>
        {
            results = await client.QueryResourceAsync(
            new ResourceIdentifier(resourceId.ToString()),
            $"{query}",
            new QueryTimeRange(TimeSpan.FromDays(7)));
        });

        return results != null && results.Value.Table.Rows.Count > 0
            ? JArray.FromObject(results.Value.Table.Rows)
            : null;
    }
}