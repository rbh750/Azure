using Common.Resources.Enums;
using Newtonsoft.Json.Linq;

namespace Service.AppInsights
{
    public interface IAppInsightsQueryService
    {
        void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);
        Task<JArray?> RunQuery(string query, AppInsightsResourceType appInsightsResourceType);
    }
}