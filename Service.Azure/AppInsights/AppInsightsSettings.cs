namespace Service.Azure.AppInsights;

public class AppInsightsSettings
{
    public string ConfigurationKey = "AppInsights";

    public string CallerEnvironment { get; set; } = default!;
    public string CallerId { get; set; } = default!;
    public string ConnectionString { get; set; } = default!;
    public bool DeveloperMode { get; set; } = default!;
}