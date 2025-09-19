using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Resources;
using Common.Resources.Enums;
using Microsoft.Extensions.Options;

namespace Service.Azure.Docker;

public class ContainerInstanceService(IOptions<DockerSettings> dockerSettings) : IContainerInstanceService
{
    /// <inheritdoc />
    public async Task CreateAndRunContainerInstanceAsync(string containerName, string assemblyName, string[]? args = null)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new ArgumentException("Assembly name cannot be null or empty.", nameof(assemblyName));
        }

        var (memoryInGB, cpu) = ExtractResourcesFromArgs(args);

        string subscriptionId = dockerSettings.Value.Docker.Registry.SubscriptionId!;
        string resourceGroupName = dockerSettings.Value.Docker.Registry.ResourceGroupName!;
        string containerGroupName = $"{dockerSettings.Value.Docker.Registry.ContainerGroupName!}-{DateTime.UtcNow.Ticks}";
        string imageName = dockerSettings.Value.Docker.Registry.Server + dockerSettings.Value.Docker.Registry.Image!;
        var location = new AzureLocation(dockerSettings.Value.Docker.Registry.Location!);

        // Specify the correct tenant ID
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = dockerSettings.Value.Docker.Registry.TenantId!
        });
        
        var armClient = new ArmClient(credential);
        var rg = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName));

        var container = new ContainerInstanceContainer(containerName, imageName,
            new ContainerResourceRequirements(
                new ContainerResourceRequestsContent(memoryInGB, cpu)
            ));

        // Add command line arguments if provided
        if (args != null && args.Length > 0)
        {
            container.Command.Clear(); // Clear default command
            container.Command.Add("dotnet");
            container.Command.Add(assemblyName);
            foreach (var arg in args)
            {
                container.Command.Add(arg);
            }
        }

        var containers = new List<ContainerInstanceContainer> { container };

        var containerGroupData = new ContainerGroupData(location, containers, ContainerInstanceOperatingSystemType.Linux)
        {
            RestartPolicy = ContainerGroupRestartPolicy.Never // run once and stop
        };

        containerGroupData.ImageRegistryCredentials.Add(new ContainerGroupImageRegistryCredential(dockerSettings.Value.Docker.Registry.Server)
        {
            Username = dockerSettings.Value.Docker.Registry.UserName!,
            Password = dockerSettings.Value.Docker.Registry.Password!
        });

        try
        {
            await rg.GetContainerGroups().CreateOrUpdateAsync(
                global::Azure.WaitUntil.Completed,
                containerGroupName,
                containerGroupData);
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException($"Failed to create container group '{containerGroupName}'. {ex.Message}", ex);
        }
    }


    /// <inheritdoc />
    public async Task<List<ContainerGroupInfo>> GetContainerGroupStatesAsync()
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = dockerSettings.Value.Docker.Registry.TenantId!
        });

        var armClient = new ArmClient(credential);
        var rg = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(
                dockerSettings.Value.Docker.Registry.SubscriptionId!,
                dockerSettings.Value.Docker.Registry.ResourceGroupName!));

        var containerGroupInfos = new List<ContainerGroupInfo>();
        var baseContainerGroupName = dockerSettings.Value.Docker.Registry.ContainerGroupName!;

        try
        {
            await foreach (var containerGroup in rg.GetContainerGroups())
            {
                // Only include container groups that match our naming pattern
                if (containerGroup.Data.Name.StartsWith(baseContainerGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    // Get fresh instance data by making a specific call for this container group
                    var freshContainerGroup = await rg.GetContainerGroupAsync(containerGroup.Data.Name);
                    var freshData = freshContainerGroup.Value.Data;
                    
                    var info = new ContainerGroupInfo
                    {
                        Name = freshData.Name,
                        State = freshData.InstanceView?.State ?? "Unknown",
                        CreatedTime = GetContainerGroupCreatedTime(freshData),
                        Containers = []
                    };

                    var containers = freshData.Containers;
                    for (int i = 0; i < containers.Count; i++)
                    {
                        var c = containers[i];
                        info.Containers.Add(new ContainerInfo
                        {
                            Name = c.Name,
                            State = freshData.InstanceView?.State ?? "Unknown",
                            RestartCount = freshData.InstanceView?.Events != null
                                ? CountPullingEvents(freshData.InstanceView.Events)
                                : 0
                        });
                    }

                    containerGroupInfos.Add(info);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve container group states. {ex.Message}", ex);
        }

        return containerGroupInfos;
    }

    // Helper method to count "Pulling" events in an indexable collection
    private static int CountPullingEvents(IReadOnlyList<ContainerEvent> events)
    {
        int count = 0;
        for (int i = 0; i < events.Count; i++)
        {
            if (events[i].Name == "Pulling")
            {
                count++;
            }
        }
        return count;
    }

    /// <inheritdoc />
    public async Task<int> CleanupCompletedContainerGroupsAsync()
    {
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = dockerSettings.Value.Docker.Registry.TenantId!
        });

        var armClient = new ArmClient(credential);
        var rg = armClient.GetResourceGroupResource(
            ResourceGroupResource.CreateResourceIdentifier(
                dockerSettings.Value.Docker.Registry.SubscriptionId!,
                dockerSettings.Value.Docker.Registry.ResourceGroupName!));

        var baseContainerGroupName = dockerSettings.Value.Docker.Registry.ContainerGroupName!;
        int deletedCount = 0;
        var cutoffTime = DateTime.UtcNow.AddHours(-24); // 24 hours ago

        try
        {
            await foreach (var containerGroup in rg.GetContainerGroups())
            {
                // Only clean up container groups that match our naming pattern
                if (containerGroup.Data.Name.StartsWith(baseContainerGroupName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Get fresh instance data by making a specific call for this container group
                        var freshContainerGroup = await rg.GetContainerGroupAsync(containerGroup.Data.Name);
                        var freshData = freshContainerGroup.Value.Data;
                        
                        var state = freshData.InstanceView?.State ?? "Unknown";
                        var createdTime = GetContainerGroupCreatedTime(freshData);
                        
                        bool shouldDelete = false;
                        
                        // Always delete containers in definitive terminal states
                        if (IsTerminalState(state))
                        {
                            shouldDelete = true;
                        }
                        // Delete Unknown state containers only if they're older than 24 hours
                        else if (state.Equals("Unknown", StringComparison.OrdinalIgnoreCase) && 
                                 createdTime.HasValue && 
                                 createdTime.Value < cutoffTime)
                        {
                            shouldDelete = true;
                        }
                        
                        if (shouldDelete)
                        {
                            try
                            {
                                await freshContainerGroup.Value.DeleteAsync(global::Azure.WaitUntil.Started);
                                deletedCount++;
                            }
                            catch
                            {
                                // Continue with other deletions if one fails
                            }
                        }
                    }
                    catch
                    {
                        // If we can't get fresh data for a specific container group, skip it
                        // This prevents one problematic container group from blocking the entire cleanup
                        continue;
                    }
                }
            }
        }
        catch
        {
            // Don't let cleanup failures block new container creation
        }

        return deletedCount;
    }

    // Gets the creation time of a container group from various sources.
    private static DateTimeOffset? GetContainerGroupCreatedTime(ContainerGroupData containerGroupData)
    {
        // Try to get creation time from events first
        if (containerGroupData.InstanceView?.Events != null && containerGroupData.InstanceView.Events.Count > 0)
        {
            return containerGroupData.InstanceView.Events[0].FirstTimestamp;
        }

        // Fallback: try to parse from container group name if it contains timestamp
        // Format: app-consolerunner-job-{DateTime.UtcNow.Ticks}
        var nameParts = containerGroupData.Name.Split('-');
        if (nameParts.Length > 0)
        {
            var lastPart = nameParts[^1]; // Get last part
            if (long.TryParse(lastPart, out var ticks))
            {
                try
                {
                    return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
                }
                catch
                {
                    // Invalid ticks value, continue to next fallback
                }
            }
        }

        // No reliable creation time found
        return null;
    }

    // Checks if the given state means the container group is finished (like Succeeded, Failed, Stopped, or Terminated).
    private static bool IsTerminalState(string state)
    {
        return state.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("Stopped", StringComparison.OrdinalIgnoreCase) ||
               state.Equals("Terminated", StringComparison.OrdinalIgnoreCase);
    }

    // Gets the CPU and memory settings from the command line arguments.
    // Returns default values if not found.
    private static (double memoryInGB, double cpu) ExtractResourcesFromArgs(string[]? args)
    {
        // Default values
        double memoryInGB = 2.0;
        double cpu = 2.0;

        if (args == null || args.Length == 0)
        {
            return (memoryInGB, cpu);
        }

        // Find containerInstanceService argument
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--containerinstanceservice", StringComparison.CurrentCultureIgnoreCase) && i + 1 < args.Length)
            {
                if (Enum.TryParse<ContainerInstanceServiceEnum>(args[i + 1], true, out var containerService))
                {
                    return GetResourcesFromEnum(containerService);
                }
                break;
            }
        }

        return (memoryInGB, cpu);
    }

    // Figures out the memory and CPU settings from the given command line arguments.
    private static (double memoryInGB, double cpu) GetResourcesFromEnum(ContainerInstanceServiceEnum containerService)
    {
        return containerService switch
        {
            ContainerInstanceServiceEnum.TwoGB_TwovCPU => (2.0, 2.0),
            ContainerInstanceServiceEnum.TwoGB_ThreevCPU => (2.0, 3.0),
            ContainerInstanceServiceEnum.TwoGB_FourvCPU => (2.0, 4.0),
            ContainerInstanceServiceEnum.FourGB_TwovCPU => (4.0, 2.0),
            ContainerInstanceServiceEnum.FourGB_ThreevCPU => (4.0, 3.0),
            ContainerInstanceServiceEnum.FourGB_FourvCPU => (4.0, 4.0),
            ContainerInstanceServiceEnum.EightGB_TwovCPU => (8.0, 2.0),
            ContainerInstanceServiceEnum.EightGB_ThreevCPU => (8.0, 3.0),
            ContainerInstanceServiceEnum.EightGB_FourvCPU => (8.0, 4.0),
            ContainerInstanceServiceEnum.SixteenGB_TwovCPU => (16.0, 2.0),
            ContainerInstanceServiceEnum.SixteenGB_ThreevCPU => (16.0, 3.0),
            ContainerInstanceServiceEnum.SixteenGB_FourvCPU => (16.0, 4.0),
            _ => (2.0, 2.0) // Default fallback
        };
    }
}
