using Entities.AppInsights;
using FluentEmail.Core;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Service.Azure.AppInsights.Mock;
using Service.Azure.RetryPolicy;

namespace Service.Azure.AppInsights;

public class AppInsightsTelemetryService : IAppInsightsTelemetryService
{
    private readonly string connectionString;
    private readonly bool developerMode;
    private readonly TelemetryClient? telemetryClient = null;
    private readonly AppInsightsTelemetryTestClient? appInsightsTelemetryTestClient = null;
    private readonly IRetryPolicyService retryPolicyService;

    public AppInsightsTelemetryService(IOptions<AppInsightsSettings> appInsightsSettings, IRetryPolicyService retryPolicyService)
    {
        connectionString = appInsightsSettings.Value.ConnectionString!;
        developerMode = appInsightsSettings.Value.DeveloperMode;

        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();

        if (telemetryConfiguration != null)
        {
            telemetryConfiguration.ConnectionString = connectionString;
            telemetryConfiguration.TelemetryChannel.DeveloperMode = developerMode;
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        this.retryPolicyService = retryPolicyService;
    }

    // Overloaded constructor for testing.
    // TelemetryClient is sealed and cannot be mocked, so use AppInsightsTelemetryTestClient for unit tests.
    // This allows you to simulate telemetry operations during testing.
    public AppInsightsTelemetryService(
        IOptions<AppInsightsSettings> appInsightsSettings,
        IRetryPolicyService retryPolicyService,
        AppInsightsTelemetryTestClient appInsightsTelemetryTestClient)
    {
        this.appInsightsTelemetryTestClient = appInsightsTelemetryTestClient;
        connectionString = appInsightsSettings.Value.ConnectionString!;
        developerMode = appInsightsSettings.Value.DeveloperMode;
        this.retryPolicyService = retryPolicyService;
    }

    /// <inheritdoc />
    public void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        retryPolicyService.Configure(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    /// <inheritdoc />
    public bool AddError(AppInsightsException appInsightsExceptionEntity)
    {
        var result = false;

        try
        {
            retryPolicyService.Run(() =>
            {
                if (telemetryClient != null)
                {
                    telemetryClient.TrackException(appInsightsExceptionEntity.Exception, appInsightsExceptionEntity.Properties);
                    if (developerMode) telemetryClient.Flush();
                    result = true;
                }
                else if (appInsightsTelemetryTestClient != null && appInsightsExceptionEntity.Exception != null)
                {
                    appInsightsTelemetryTestClient.TrackException(appInsightsExceptionEntity.Exception, appInsightsExceptionEntity.Properties);
                    if (developerMode) appInsightsTelemetryTestClient.Flush();
                    result = true;
                }
            });

        }
        catch { }

        return result;
    }

    /// <inheritdoc />
    public bool AddTrace(AppInsightsTrace appInsightsTraceEntity)
    {
        var result = false;

        try
        {
            retryPolicyService.Run(() =>
            {
                if (telemetryClient != null)
                {
                    telemetryClient.TrackTrace(appInsightsTraceEntity.Message, SeverityLevel.Information, appInsightsTraceEntity.Properties);
                    if (developerMode) telemetryClient.Flush();
                    result = true;
                }
                else if (appInsightsTelemetryTestClient != null)
                {
                    appInsightsTelemetryTestClient.TrackTrace(appInsightsTraceEntity.Message, SeverityLevel.Information, appInsightsTraceEntity.Properties);
                    if (developerMode) appInsightsTelemetryTestClient.Flush();
                    result = true;
                }
            });
        }
        catch { }

        return result;
    }

    /// <inheritdoc />
    public bool AddHttpRequest(AppInsightsHttpRequest appInsightsHttpRequestEntity)
    {
        var result = false;

        try
        {
            var request = new RequestTelemetry
            {
                Name = appInsightsHttpRequestEntity.Name,
                Timestamp = appInsightsHttpRequestEntity.StartTime,
                Duration = appInsightsHttpRequestEntity.Duration,
                ResponseCode = appInsightsHttpRequestEntity.ResponseCode,
                Success = appInsightsHttpRequestEntity.Success
            };

            appInsightsHttpRequestEntity.Properties.ForEach(kvp =>
            {
                request.Properties.Add(kvp.Key, kvp.Value);
            });

            retryPolicyService.Run(() =>
            {
                if (telemetryClient != null)
                {
                    telemetryClient.TrackRequest(request);
                    if (developerMode) telemetryClient.Flush();
                    result = true;
                }
                else if (appInsightsTelemetryTestClient != null)
                {
                    appInsightsTelemetryTestClient.TrackRequest(request);
                    if (developerMode) appInsightsTelemetryTestClient.Flush();
                    result = true;
                }
            });
        }
        catch { }

        return result;
    }
}