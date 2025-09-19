namespace Service.Azure.Docker;

public class DockerSettings
{
    public string ConfigurationKey = "Docker";
    public Docker Docker { get; set; } = default!;
}

public class Docker
{
    public Registry Registry { get; set; } = default!;
}

public class Registry
{
    public string? ContainerGroupName { get; set; } 
    public string? Image { get; set; } 
    public string? Location { get; set; }
    public string? Password { get; set; } 
    public string? ResourceGroupName { get; set; }
    public string? Server { get; set; }
    public string? SubscriptionId { get; set; }
    public string? TenantId { get; set; }
    public string? UserName { get; set; } 
}

