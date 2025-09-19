using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Service.Azure.Entra;

namespace Service.Azure.KeyVault;

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient secretClient;

    public KeyVaultService(IOptions<EntraSettings> entraSettings)
    {
        var keyVaultSettings = entraSettings.Value.KeyVaultAuth;

        if (string.IsNullOrEmpty(keyVaultSettings.KeyVaultUrl))
            throw new ArgumentException("KeyVault URL is required");

        if (string.IsNullOrEmpty(keyVaultSettings.ClientId))
            throw new ArgumentException("KeyVault ClientId is required");

        if (string.IsNullOrEmpty(keyVaultSettings.Secret))
            throw new ArgumentException("KeyVault Secret is required");

        if (string.IsNullOrEmpty(keyVaultSettings.TenantId))
            throw new ArgumentException("KeyVault TenantId is required");

        var credential = new ClientSecretCredential(
            keyVaultSettings.TenantId,
            keyVaultSettings.ClientId,
            keyVaultSettings.Secret);

        secretClient = new SecretClient(new Uri(keyVaultSettings.KeyVaultUrl), credential);
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            var response = await secretClient.GetSecretAsync(secretName);
            return response.Value.Value;
        }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Secret not found
            return null;
        }
    }
}
