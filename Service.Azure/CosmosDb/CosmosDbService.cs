using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Service.Azure.RetryPolicy;
using System.Reflection;

namespace Service.Azure.CosmosDb;

// This class is pPrimarily intended for eTag-based optimistic concurrency control.
public class CosmosDbService(IOptions<CosmosDbSettings> cosmosDbSettings, IRetryPolicyService retryPolicyService) : ICosmosDbService
{
    private readonly string connectionString = cosmosDbSettings.Value.ConnectionString;
    private readonly string databaseName = cosmosDbSettings.Value.DatabaseName;
    private readonly IRetryPolicyService retryPolicyService = retryPolicyService;
    private Container? targetContainer;

    /// <inheritdoc />
    public Exception OperationError { get; private set; } = default!;

    /// <inheritdoc />
    public void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        retryPolicyService.Configure(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    /// <inheritdoc />
    public async Task<T?> Get<T>(string containerReference, object partitionKeyValue, string recordId) where T : new()
    {
        T? result = default;

        var containerName = cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.Id
            ?? throw ThrowError(CosmosDbServiceError.Container);

        try
        {
            targetContainer = TargetContainer(containerName);

            if (targetContainer != null)
            {
                await retryPolicyService.RunAsync(async () =>
                {
                    ItemResponse<T> response = await targetContainer.ReadItemAsync<T>(recordId, GetPartitionKey(partitionKeyValue));
                    result = response.Resource;
                });
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
            result = default;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<T>?> GetAll<T>(string query, string containerReference) where T : new()
    {
        var result = default(List<T>);

        var containerName = cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.Id
            ?? throw ThrowError(CosmosDbServiceError.Container);

        try
        {
            targetContainer = TargetContainer(containerName);

            if (targetContainer != null)
            {
                await retryPolicyService.RunAsync(async () =>
                {
                    QueryDefinition queryDefinition = new(query);
                    FeedIterator<T>? queryResultGetIterator = targetContainer.GetItemQueryIterator<T>(queryDefinition);

                    while (queryResultGetIterator.HasMoreResults)
                    {
                        FeedResponse<T> responseData = await queryResultGetIterator.ReadNextAsync();
                        result = [.. responseData];
                    }
                });
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> Upsert<T>(T entity, string containerReference, int? timeToLive = null) where T : new()
    {
        bool result = true;

        var containerName = cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.Id
            ?? throw ThrowError(CosmosDbServiceError.Container);

        // If TTL is not specified, get it from the container settings.
        timeToLive ??= cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.TimeToLIve;

        try
        {
            targetContainer = TargetContainer(containerName);
            var (partitionKey, idValue) = GetPartitionKeyAnIdValue(entity, containerReference);

            if (targetContainer != null)
            {
                // Add TTL property dynamically if specified.
                if (timeToLive.HasValue)
                {
                    typeof(T).GetProperty("TimeToLive")?
                        .SetValue(entity, timeToLive.Value);
                }

                ItemResponse<T>? response = null;

                await retryPolicyService.RunAsync(async () =>
                {
                    response = await targetContainer.UpsertItemAsync(entity, partitionKey);
                });

                if (response is { StatusCode: System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created })
                {
                    result = true;
                }
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
            result = false;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> PatchAtomicAsync(Dictionary<string, object> fieldValues, string containerReference, object partitionKeyValue, string recordId)
    {
        bool result = false;

        var containerName = cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.Id
            ?? throw ThrowError(CosmosDbServiceError.Container);

        try
        {
            targetContainer = TargetContainer(containerName);

            if (targetContainer != null && fieldValues.Count > 0)
            {
                ItemResponse<dynamic>? response = null;

                await retryPolicyService.RunAsync(async () =>
                {
                    // Get fresh data on each retry attempt - this makes ALL operations atomic
                    var currentItem = await targetContainer.ReadItemAsync<dynamic>(recordId, GetPartitionKey(partitionKeyValue));
                    
                    // Create patch operations with the provided field values
                    var patchOperations = new List<PatchOperation>();
                    
                    foreach (var field in fieldValues)
                    {
                        object newValue;

                        // Keeps complex property navigation logic out of the repository layer.
                        if (field.Value is Func<dynamic, object> calculationFunction)
                        {
                            newValue = calculationFunction(currentItem.Resource);
                        }

                        // Handle direct value assignment.
                        else
                        {
                            newValue = field.Value;
                        }
                        
                        patchOperations.Add(PatchOperation.Replace($"/{field.Key}", newValue));
                    }

                    // IfMatchEtag: Sets the ETag value that must match the current item's ETag
                    // IfNoneMatchEtag: Sets the ETag value that must NOT match the current item's ETag
                    // EnableContentResponseOnWrite: Controls whether the response includes the updated item
                    // IndexingDirective: Specifies how the item should be indexed
                    // SessionToken: For session consistency levels
                    // ConsistencyLevel: Override the default consistency level for this operation

                    var requestOptions = new PatchItemRequestOptions
                    {
                        IfMatchEtag = currentItem.ETag // Use fresh ETag for atomic operation
                    };

                    response = await targetContainer.PatchItemAsync<dynamic>(
                        recordId,
                        GetPartitionKey(partitionKeyValue),
                        patchOperations,
                        requestOptions);
                });

                result = response is { StatusCode: System.Net.HttpStatusCode.OK };
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            OperationError = new InvalidOperationException("Concurrency conflict: Record was modified by another process", ex);
            result = false;
        }
        catch (Exception ex)
        {
            OperationError = ex;
            result = false;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> Delete<T>(T entity, string containerReference) where T : new()
    {
        bool result = false;

        var containerName = cosmosDbSettings.Value
            .Containers.FirstOrDefault(x => x.Reference == containerReference)?.Id
            ?? throw ThrowError(CosmosDbServiceError.Container);

        try
        {
            targetContainer = TargetContainer(containerName);
            var (partitionKey, idValue) = GetPartitionKeyAnIdValue(entity, containerReference);

            if (targetContainer != null)
            {
                ItemResponse<T>? response = null;

                await retryPolicyService.RunAsync(async () =>
                {
                    response = await targetContainer.DeleteItemAsync<T>(idValue, partitionKey);
                });

                result = response is { StatusCode: System.Net.HttpStatusCode.NoContent };
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return result;
    }

    private Container? TargetContainer(string containerName)
    {
        var targetCosmosClient = new CosmosClient(connectionString, new CosmosClientOptions()
        {
            ConnectionMode = ConnectionMode.Gateway
        });

        return targetCosmosClient.GetContainer(databaseName, containerName);
    }

    // Finds the partition key and id value from the entity using its properties and container settings.
    // Throws an error if either is missing or invalid.
    private (PartitionKey partitionKey, string? idValue) GetPartitionKeyAnIdValue<T>(T entity, string containerReference)
    {
        PartitionKey partitionKey = default;
        string? idValue = null;
        Type type = typeof(T);

        JsonPropertyAttribute? jsonAttribute;

        var pkName = cosmosDbSettings.Value.
            Containers.FirstOrDefault(x => x.Reference == containerReference)?.DefaultPartitionKey
            ?? throw ThrowError(CosmosDbServiceError.Container);

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();
            var propVal = property.GetValue(entity);

            // The id property of a record in Azure Cosmos DB must be a string.
            if (jsonAttribute is { PropertyName: "id" } && property.GetValue(entity) is { } idPropertyValue)
            {
                idValue = Convert.ToString(idPropertyValue);
            }

            // Get partition partition key from JsonProperty attribute
            if (jsonAttribute != null && jsonAttribute.PropertyName == pkName && propVal != null)
            {
                partitionKey = GetPartitionKey(propVal);
            }
        }

        if (idValue == default)
        {
            throw new NotSupportedException($"Cosmos Db service, the entity is not valid. Cannot find the id property.");
        }

        if (partitionKey == default)
        {
            throw new NotSupportedException($"Cosmos Db service, the entity is not valid. Cannot find the partition key property."
                + $" Ensure the property defined as the partition key is present in the item and contains a valid value.");
        }

        return (partitionKey, idValue);
    }

    // Makes a PartitionKey object from the given value.
    // Throws an error if the value type is not supported.
    private static PartitionKey GetPartitionKey(object? propertyValue)
    {
        return propertyValue switch
        {
            bool boolValue => new PartitionKey(boolValue),
            string stringValue => new PartitionKey(stringValue),
            short shortValue => new PartitionKey(shortValue),
            int intValue => new PartitionKey(intValue),
            long longValue => new PartitionKey(longValue),
            _ => throw new NotSupportedException($"Cosmos Db service, unsupported property value for partition key.")
        };
    }

    // Helper method to throw an error with a message based on the type of CosmosDbServiceError.
    private static ArgumentException ThrowError(CosmosDbServiceError error)
    {
        return error switch
        {
            CosmosDbServiceError.Container => throw new ArgumentException("Container id cannot be null."),
            CosmosDbServiceError.PartitionKey => throw new ArgumentException("Partition Key cannot be null."),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }

    private enum CosmosDbServiceError
    {
        Container,
        PartitionKey,
    }
}
