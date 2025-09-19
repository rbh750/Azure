namespace Service.Azure.Docker;

/// <summary>
/// Information about an individual container
/// </summary>
public class ContainerInfo
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int RestartCount { get; set; }
}