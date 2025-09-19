
namespace Service.Azure.KeyVault
{
    public interface IKeyVaultService
    {
        /// <summary>
        /// Retrieves the value of a secret from Azure Key Vault by its name.
        /// Returns null if the secret does not exist.
        /// </summary>
        Task<string?> GetSecretAsync(string secretName);
    }
}