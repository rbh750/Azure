using Microsoft.ApplicationInsights.DataContracts;

namespace Service.Azure.AppInsights.Mock;

// Microsoft.ApplicationInsights.TelemetryClient is sealed, meaning it cannot be inherited or mocked directly.
public class AppInsightsTelemetryTestClient
{
    public List<ExceptionTelemetry> TrackedExceptions { get; } = [];
    public List<TraceTelemetry> TrackedTraces { get; } = [];
    public List<RequestTelemetry> TrackedRequests { get; } = [];
    public bool Flushed { get; private set; }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        TrackedExceptions.Add(new ExceptionTelemetry(exception)
        {
            Properties = { }
        });
        if (properties != null)
        {
            foreach (var kvp in properties)
                TrackedExceptions[^1].Properties[kvp.Key] = kvp.Value;

            // [^1] means accessing the last element of an array or list.
        }
    }

    public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string>? properties)
    {
        var trace = new TraceTelemetry(message, severityLevel);
        if (properties != null)
        {
            foreach (var kvp in properties)
                trace.Properties[kvp.Key] = kvp.Value;
        }
        TrackedTraces.Add(trace);
    }

    public void TrackRequest(RequestTelemetry request)
    {
        TrackedRequests.Add(request);
    }

    public void Flush()
    {
        Flushed = true;
    }
}
