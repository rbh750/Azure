using Entities.AppInsights;

namespace Service.Azure.AppInsights
{
    /// <summary>
    /// Interface for Application Insights telemetry services
    /// </summary>
    public interface IAppInsightsTelemetryService
    {
        /// <summary>
        /// Configures the retry policy
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="delayMilliseconds">Initial delay in milliseconds</param>
        /// <param name="maxDelayMilliseconds">Maximum delay in milliseconds</param>
        void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);

        /// <summary>
        /// Adds an error to Application Insights
        /// </summary>
        /// <param name="appInsightsExceptionEntity">The exception to track</param>
        /// <returns>True if successful, otherwise false</returns>
        bool AddError(AppInsightsException appInsightsExceptionEntity);

        /// <summary>
        /// Adds a trace to Application Insights
        /// </summary>
        /// <param name="appInsightsTraceEntity">The trace to track</param>
        /// <returns>True if successful, otherwise false</returns>
        bool AddTrace(AppInsightsTrace appInsightsTraceEntity);

        /// <summary>
        /// Adds an HTTP request to Application Insights
        /// </summary>
        /// <param name="appInsightsHttpRequestEntity">The HTTP request to track</param>
        /// <returns>True if successful, otherwise false</returns>
        bool AddHttpRequest(AppInsightsHttpRequest appInsightsHttpRequestEntity);
    }
}