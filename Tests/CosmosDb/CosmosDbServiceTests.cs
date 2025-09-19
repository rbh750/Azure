using Entities.Cosmos;
using Service.Azure.CosmosDb;
using Xunit.Abstractions;

namespace Tests.CosmosDb;

public class CosmosDbServiceTests(ICosmosDbService cosmosDbService, ITestOutputHelper output)
{
    private const string CONTAINER_REFERENCE = "UserAccounts";

    [Fact]
    public async Task UserAccount_All_CRUD_Operations_Should_Complete_Successfully()
    {
        // Arrange
        var testEmail = $"test-{Guid.NewGuid()}@example.com";

        var partitionKey = DateTime.Now.Ticks.ToString();

        var userAccount = new UserAccount
        {
            Email = testEmail,
            PartitionKey = partitionKey,
            Name = "Test User"
        };

        try
        {
            // 1. UPSERT (Create)
            output.WriteLine($"1. Testing Upsert (Create) operation for user: {testEmail}");
            var upsertResult = await cosmosDbService.Upsert(userAccount, CONTAINER_REFERENCE);
            Assert.True(upsertResult, $"Upsert operation failed. Error: {cosmosDbService.OperationError?.Message}");
            output.WriteLine("Upsert (Create) operation completed successfully");

            // 2. GET
            output.WriteLine($"2. Testing Get operation for user: {testEmail}");
            var retrievedUser = await cosmosDbService.Get<UserAccount>(CONTAINER_REFERENCE, partitionKey, testEmail);
            Assert.NotNull(retrievedUser);
            Assert.Equal(testEmail, retrievedUser.Email);
            Assert.Equal(partitionKey, retrievedUser.PartitionKey);
            Assert.Equal("Test User", retrievedUser.Name);
            output.WriteLine("Get operation completed successfully");

            // 3. UPDATE (using PatchAtomicAsync)
            output.WriteLine($"3. Testing Update operation for user: {testEmail}");
            var updateFields = new Dictionary<string, object>
            {
                { "name", "Updated Test User" }
            };
            var updateResult = await cosmosDbService.PatchAtomicAsync(updateFields, CONTAINER_REFERENCE, partitionKey, testEmail);
            Assert.True(updateResult, $"Update operation failed. Error: {cosmosDbService.OperationError?.Message}");
            
            // Verify the update
            var updatedUser = await cosmosDbService.Get<UserAccount>(CONTAINER_REFERENCE, partitionKey, testEmail);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated Test User", updatedUser.Name);
            output.WriteLine("Update operation completed successfully");

            // 4. UPSERT (Update existing)
            output.WriteLine($"4. Testing Upsert (Update) operation for user: {testEmail}");
            userAccount.Name = "Final Updated Name";
            var upsertUpdateResult = await cosmosDbService.Upsert(userAccount, CONTAINER_REFERENCE);
            Assert.True(upsertUpdateResult, $"Upsert update operation failed. Error: {cosmosDbService.OperationError?.Message}");
            
            // Verify the upsert update
            var finalUser = await cosmosDbService.Get<UserAccount>(CONTAINER_REFERENCE, partitionKey, testEmail);
            Assert.NotNull(finalUser);
            Assert.Equal("Final Updated Name", finalUser.Name);
            output.WriteLine("Upsert (Update) operation completed successfully");

            output.WriteLine("All CRUD operations completed successfully!");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Test failed with error: {ex.Message}");
            throw;
        }
        finally
        {
            // 5. DELETE - Always cleanup regardless of test outcome
            output.WriteLine($"5. Cleaning up - deleting user: {testEmail}");
            try
            {
                var deleteResult = await cosmosDbService.Delete(userAccount, CONTAINER_REFERENCE);
                if (deleteResult)
                {
                    output.WriteLine("Delete operation completed successfully");
                }
                else
                {
                    output.WriteLine($"Delete operation failed. Error: {cosmosDbService.OperationError?.Message}");
                }
            }
            catch (Exception deleteEx)
            {
                output.WriteLine($"Cleanup failed with exception: {deleteEx.Message}");
            }
        }
    }
}