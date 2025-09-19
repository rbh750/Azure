using Common.Resources.Enums;
using Entities.AppInsights;
using Service.Azure.AppInsights;

namespace Tests.AppInsights;

public class AppInsightsTelemetryServiceTests(IAppInsightsTelemetryService appInsightsTelemetryService)
{
    const string TEST = "INTEGRATION TEST";

    readonly AppInsightsHttpRequest appInsightsHttpRequestEntity = new(AppInsightsSource.Tests, "userId", "Get", "url", "clientId", "dev")
    {
        StartTime = DateTime.UtcNow,
        Duration = TimeSpan.FromSeconds(1),
        Name = "XTest",
        ResponseCode = "200",
        Success = true
    };

    [Fact]
    public void Add_Trace_ReturnsTrue()
    {
        AppInsightsTrace appInsightsEntity = new(AppInsightsSource.Tests, TEST);
        appInsightsEntity.AddProperty("prop key3", "prop key3");
        Assert.True(appInsightsTelemetryService.AddTrace(appInsightsEntity));
    }

    [Fact]
    public void Add_HttpRequest_ReturnsTrue()
    {
        Assert.True(appInsightsTelemetryService.AddHttpRequest(appInsightsHttpRequestEntity));
    }

    [Fact]
    public void Add_AddError_ReturnsTrue()
    {
        AppInsightsException appInsightsExceptionEntity = new(AppInsightsSource.Tests)
        {
            Exception = new Exception(TEST)
        };

        Assert.True(appInsightsTelemetryService.AddError(appInsightsExceptionEntity));
    }
}