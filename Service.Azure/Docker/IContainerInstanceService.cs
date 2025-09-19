namespace Service.Azure.Docker;

public interface IContainerInstanceService
{
    /// <summary>
    /// Deploys an existing container image from Azure Container Registry to an Azure Container Instance and runs it with the given name and optional arguments.
    /// One argument must be convertible to ContainerInstanceServiceEnum, which sets the container's CPU and memory.
    /// 
    /// Example args:
    /// <code>
    /// string[] args = {
    ///     "--name", "my-job",
    ///     "--containerInstanceService", nameof(ContainerInstanceServiceEnum.TwoGB_TwovCPU),
    ///     "--input", "path/to/input"
    /// };
    /// </code>
    /// </summary>
    /// <param name="containerName">The name of the container to create</param>
    /// <param name="assemblyName">The .NET assembly name to execute (e.g., "App.ConsoleRunner.dll")</param>
    /// <param name="args">Optional command line arguments to pass to the container application</param>
    /// <exception cref="ArgumentException">Thrown when assemblyName is null or empty</exception>
    Task CreateAndRunContainerInstanceAsync(string containerName, string assemblyName, string[]? args = null);
    
    /// <summary>
    /// Gets a list of all container groups in the resource group and shows their current states.
    /// </summary>
    /// <returns>A list of container group information including their states and containers</returns>
    Task<List<ContainerGroupInfo>> GetContainerGroupStatesAsync();
    
    /// <summary>
    /// Deletes container groups that are finished, failed, stopped, terminated, or unknown and older than 24 hours.
    /// </summary>
    /// <returns>The number of container groups that were deleted</returns>
    Task<int> CleanupCompletedContainerGroupsAsync();
}