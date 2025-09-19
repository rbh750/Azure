using Common.Resources.Enums;

namespace Entities.AppInsights;
public class AppInsightsHttpRequest
{
    public required DateTimeOffset StartTime { get; set; }
    public required TimeSpan Duration { get; set; }
    public required string Name { get; set; }
    public required string ResponseCode { get; set; }
    public required bool Success { get; set; }

    public Dictionary<string, string> Properties;

    public AppInsightsHttpRequest(
        AppInsightsSource source, 
        string? userId, 
        string verb, 
        string url, 
        string clientId, 
        string environment)
    {
        var src = Enum.GetName(source) ?? throw new ArgumentNullException(nameof(source));

        Properties = new Dictionary<string, string>
        {
            { "Source", src },
            { "UserId", userId ?? "NA" },
            { "Verb", verb },
            { ErrorPropType.Environment.ToString(), environment }
        };
    }

    public void AddProperty(string key, string value)
    {
        Properties.TryAdd(key, value);
    }
}

