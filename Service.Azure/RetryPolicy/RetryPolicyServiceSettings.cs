namespace Service.Azure.RetryPolicy;

public class RetryPolicyServiceSettings
{
    public string ConfigurationKey = "RetryPolicy";
    public int MaxRetries { get; set; } = 2;
    public int DelayMilliseconds { get; set; } = 100;
    public int MaxDelayMilliseconds { get; set; } = 1000;
}

