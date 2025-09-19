namespace Service.Azure.AppInsights;

public class AppInsightsSettings
{
    public string ConfigurationKey = "AppInsights";

    public string CallerEnvironment { get; set; } = default!;
    public string CallerId { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string ConnectionString { get; set; } = default!;
    public bool DeveloperMode { get; set; } = default!;
    public Resources Resources { get; set; } = default!;
}


public class Resources
{
    public string ResourceGroupName { get; set; } = default!;
    public string ResourceNameApi { get; set; } = default!;
    public string ResourceNameWebJobs { get; set; } = default!;
    public string SubscriptionId { get; set; } = default!;
}

