namespace Service.Azure.Entra;

public record EntraSettings
{
    public string ConfigurationKey { get; set; } = "Entra";
    public PublicAuthSettings PublicAuth { get; set; } = new();
    public KeyVaultAppAndUrl KeyVaultAuth { get; set; } = new();
    public AppInsightsApp AppInsightsAuth { get; set; } = new();
}

public record PublicAuthSettings
{
    public string ClientId { get; set; } = default!;
}

public record KeyVaultAppAndUrl
{
    public string ClientId { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string KeyVaultUrl { get; set; } = default!;
}

public record AppInsightsApp
{
    public string ClientId { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public string TenantId { get; set; } = default!;
}
