using Common.Resources.Enums;

namespace Entities.AppInsights;

public class AppInsightsTrace
{
    private readonly Dictionary<string, string> properties;

    public string Message { get; private set; } = string.Empty;
    public Exception? Exception { get; set; }
    public Dictionary<string, string> Properties
    {
        get => properties;
    }

    public AppInsightsTrace(AppInsightsSource source, string message)
    {
        var src = Enum.GetName(source) ?? throw new ArgumentNullException(nameof(source));

        properties = new Dictionary<string, string>
        {
            { "Source", src }
        };

        Message = message;
    }

    public void AddProperty(string key, string value)
    {
        properties.Add(key, value);
    }
}
