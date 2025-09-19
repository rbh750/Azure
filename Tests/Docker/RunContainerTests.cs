using Service.Azure.Docker;
using Xunit.Abstractions;
using Common.Resources.Enums;

namespace Tests.Docker;

/// <summary>
/// Integration tests for ContainerInstanceService.
/// 
/// Prerequisites:
/// 1. Create a simple console application
/// 2. Containerize it and push the image to Azure Container Registry
/// 3. The ContainerInstanceService creates a container group and runs the image directly from the registry
/// 
/// Note: These tests will create actual Azure Container Instances and may incur costs.
/// They also require proper Azure credentials and configuration.
/// </summary>
public class RunContainerTests(IContainerInstanceService containerInstanceService, ITestOutputHelper output)
{
    private const string DEFAULT_ASSEMBLY_NAME = "MyConsoleApp.dll"; // Change this to your actual assembly name.

    [Fact]
    public async Task RunContainerWithCompleteWorkflow()
    {
        // Arrange
        var containerName = $"test-workflow-{Guid.NewGuid().ToString()[..8]}";
        var args = new[]
        {
            "--name", "integration-test",
            "--containerInstanceService", nameof(ContainerInstanceServiceEnum.TwoGB_TwovCPU),
            "--testMode", "true"
        };

        output.WriteLine($"Starting complete container workflow test with: {containerName}");
        output.WriteLine($"Assembly: {DEFAULT_ASSEMBLY_NAME}");
        output.WriteLine($"Args: {string.Join(" ", args)}");

        try
        {
            // Step 1: Create and run container
            output.WriteLine("Step 1: Creating and running container instance");
            await containerInstanceService.CreateAndRunContainerInstanceAsync(containerName, DEFAULT_ASSEMBLY_NAME, args);
            output.WriteLine("Container instance created successfully");

            // Step 2: Wait for container to start
            output.WriteLine("Step 2: Waiting for container to start");
            await Task.Delay(TimeSpan.FromSeconds(45));

            // Step 3: Verify container was created
            output.WriteLine("Step 3: Verifying container creation");
            var containerGroups = await containerInstanceService.GetContainerGroupStatesAsync();
            var createdContainer = containerGroups.FirstOrDefault(cg => 
                cg.Containers.Any(c => c.Name == containerName));

            Assert.NotNull(createdContainer);
            Assert.Contains(createdContainer.Containers, c => c.Name == containerName);
            output.WriteLine($"Container verified - State: {createdContainer.State}, Containers: {createdContainer.Containers.Count}");

            // Step 4: Wait for container to potentially complete
            output.WriteLine("Step 4: Waiting for container to complete");
            await Task.Delay(TimeSpan.FromSeconds(60));

            // Step 5: Run cleanup
            output.WriteLine("Step 5: Running cleanup of completed containers");
            var deletedCount = await containerInstanceService.CleanupCompletedContainerGroupsAsync();
            output.WriteLine($"Cleanup completed - Deleted {deletedCount} container groups");

            // Step 6: Verify final state
            output.WriteLine("Step 6: Verifying final state");
            var finalStates = await containerInstanceService.GetContainerGroupStatesAsync();
            var remainingContainer = finalStates.FirstOrDefault(cg => 
                cg.Containers.Any(c => c.Name == containerName));

            if (remainingContainer == null)
            {
                output.WriteLine("Container was successfully cleaned up (completed and removed)");
            }
            else
            {
                output.WriteLine($"Container still exists in state: {remainingContainer.State} (likely still running)");
            }

            output.WriteLine("Complete container workflow test completed successfully!");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Container workflow test failed: {ex.Message}");
            
            // Attempt cleanup on failure
            try
            {
                output.WriteLine("Attempting cleanup after test failure");
                await containerInstanceService.CleanupCompletedContainerGroupsAsync();
                output.WriteLine("Emergency cleanup completed");
            }
            catch (Exception cleanupEx)
            {
                output.WriteLine($"Emergency cleanup also failed: {cleanupEx.Message}");
            }
            
            throw;
        }
    }
}
