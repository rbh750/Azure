using Common.Resources.Enums;

namespace Entities.AppInsights;

public class AppInsightsException
{
    private readonly Dictionary<string, string> properties;

    public Exception? Exception { get; set; }
    public Dictionary<string, string> Properties
    {
        get => properties;
    }

    public AppInsightsException(AppInsightsSource source)
    {
        var src = Enum.GetName(source) ?? throw new ArgumentNullException(nameof(source));

        properties = new Dictionary<string, string>
        {
            { "Source", src }
        };
    }

    public void AddProperty(string key, string value)
    {
        properties.TryAdd(key, value);
    }
}

