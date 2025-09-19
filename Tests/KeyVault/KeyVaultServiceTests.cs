using Service.Azure.KeyVault;
using Xunit.Abstractions;

namespace Tests.KeyVault
{
    /// <summary>
    /// Integration test for Azure KeyVault GetSecretAsync
    /// </summary>
    public class KeyVaultServiceTests(IKeyVaultService keyVaultService, ITestOutputHelper output)
    {
        private readonly IKeyVaultService keyVaultService = keyVaultService;
        private readonly ITestOutputHelper output = output;

        [Fact]
        public async Task GetSecretAsync_ReturnsSecretValue()
        {
            // Replace with a valid secret name that exists in your Key Vault for testing
            const string secretName = "TestSecret";

            output.WriteLine($"Attempting to retrieve secret: {secretName}");
            var secretValue = await keyVaultService.GetSecretAsync(secretName);

            output.WriteLine($"Secret value: {secretValue}");
            Assert.False(string.IsNullOrEmpty(secretValue));
        }
    }
}
