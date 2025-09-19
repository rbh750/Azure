namespace Service.Azure.Entra;

public record EntraSettings
{
    public string ConfigurationKey { get; set; } = "Entra";
    public PublicAuthSettings PublicAuth { get; set; } = new();
    public KeyVaultAuthSettings KeyVaultAuth { get; set; } = new();
}

public record PublicAuthSettings
{
    public string ClientId { get; set; } = default!;
}

public record KeyVaultAuthSettings
{
    public string ClientId { get; set; } = default!;
    public string Secret { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string KeyVaultUrl { get; set; } = default!;
}
