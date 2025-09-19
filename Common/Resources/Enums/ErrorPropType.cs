namespace Common.Resources.Enums;

/// <summary>
/// Defines standardized property keys used across Application Insights telemetry entities.
/// Primarily used by AppInsightsHttpRequest to ensure consistent property naming for telemetry data.
/// </summary>
public enum ErrorPropType
{
    Method,
    PartitionKey,
    Rowkey,
    TableName,
    Environment,
    Etc
}

