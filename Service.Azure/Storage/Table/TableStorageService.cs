using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Service.Azure.RetryPolicy;
using System.Text;

namespace Service.Azure.Storage.Table;

// https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/tables/Azure.Data.Tables/samples
/// <summary>
/// Service for working with Azure Table Storage to store and retrieve data records.
/// </summary>
public class TableStorageService(
    IOptions<StorageSettings> storageSettings,
    IRetryPolicyService retryPolicyService) : ITableStorageService
{
    private readonly IRetryPolicyService retryPolicyService = retryPolicyService;
    private readonly Lock lockTable = new();
    private readonly string connectionString = storageSettings.Value.ConnectionString;
    private readonly string nullTableCientError = "The table client has not been initialized.";
    private TableClient? tableClient;

    public Exception OperationError { get; private set; } = default!;

    /// <inheritdoc />
    public void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        retryPolicyService.Configure(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    /// <inheritdoc />
    public void InitializeTableClient(string tableName, bool checkTable = false)
    {
        if (tableName == null)
        {
            return;
        }

        if (checkTable)
        {
            CheckIfTableExists(tableName);
        }

        lock (lockTable)
        {
            tableClient = new TableClient(connectionString, tableName);
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddRecord<T>(string? tableName, T entity) where T : class, ITableEntity, new()
    {
        Response? response = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                if (tableName != null)
                {
                    tableClient = new TableClient(connectionString, tableName);
                }
                else
                {
                    CheckTableClient();
                }

                response = await tableClient!.AddEntityAsync(entity);
            });

            return response != null && response.Status == 204;
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> CountRecords(string tableName, string filter)
    {
        CheckIfTableExists(tableName);
        CheckTableClient();

        int records = 0;

        try
        {
            // Stream records asynchronously without manual continuation token handling
            await foreach (var entity in tableClient!.QueryAsync<TableEntity>(filter))
            {
                records++;
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return records;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRecord(string? tableName, string partitionKey, string rowKey)
    {
        bool result = true;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                if (tableName != null)
                {
                    tableClient = new TableClient(connectionString, tableName);
                }
                else
                {
                    CheckTableClient();
                }

                var response = await tableClient!.DeleteEntityAsync(partitionKey, rowKey);
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
            result = false;
        }

        return result;

        // For the current assesmbly, DeleteEntityAsync is not returning a response.
        // return response != null && response.Status == 204;
    }

    /// <inheritdoc />
    public async Task<T?> GetRecord<T>(string? tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
    {
        T? result = default;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                if (tableName != null)
                {
                    tableClient = new TableClient(connectionString, tableName);
                }
                else
                {
                    CheckTableClient();
                }

                result = (await tableClient!.GetEntityAsync<T>(partitionKey, rowKey)).Value;
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<T>> GetRecords<T>(string? tableName, string filter, int maxRecordsPerPage)
        where T : class, ITableEntity, new()
    {
        // Validate max records per page
        if (maxRecordsPerPage > 1000)
        {
            throw new InvalidOperationException("The maximum number of records per page that can be added to the filter is 1000.");
        }

        // Ensure at least one filtering condition is specified
        if (string.IsNullOrEmpty(filter))
        {
            throw new InvalidOperationException("The filter must be specified.");
        }

        var records = new List<T>();
        var queryFilter = new StringBuilder();

        try
        {
            if (!string.IsNullOrEmpty(filter))
            {
                if (queryFilter.Length > 0)
                {
                    queryFilter.Append(" and ");
                }
                queryFilter.Append(filter);
            }

            await retryPolicyService.RunAsync(async () =>
            {
                tableClient ??= tableName != null
                    ? new TableClient(connectionString, tableName)
                    : throw new InvalidOperationException("Table client is not initialized.");

                await foreach (var entity in tableClient!.QueryAsync<T>(queryFilter.ToString(), maxPerPage: maxRecordsPerPage))
                {
                    records.Add(entity);
                }
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return records;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateRecord(
        string? tableName, 
        string partitionKey, 
        string rowKey, 
        Dictionary<string, object?> propertiesAndValues, 
        bool useOptimisticConcurrency = true)
    {
        Response? response = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                if (tableName != null)
                {
                    tableClient = new TableClient(connectionString, tableName);
                }
                else
                {
                    CheckTableClient();
                }

                // Get the current entity with its ETag for each retry attempt
                TableEntity entity = await tableClient!.GetEntityAsync<TableEntity>(partitionKey, rowKey);

                // Update the entity properties
                foreach (var kv in propertiesAndValues)
                {
                    entity[kv.Key] = kv.Value;
                }

                // Use ETag for optimistic concurrency control or ETag.All to bypass
                ETag etagToUse = useOptimisticConcurrency ? entity.ETag : ETag.All;
                response = await tableClient.UpdateEntityAsync(entity, etagToUse);
            });

            return response != null && response.Status == 204;
        }
        catch (RequestFailedException ex) when (ex.Status == 412) // Precondition Failed
        {
            // ETag mismatch - this will be caught by the retry policy and retried with fresh ETag
            OperationError = new InvalidOperationException("Concurrency conflict: Entity was modified by another process", ex);
            return false;
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpsertRecord<T>(string? tableName, T entity) where T : class, ITableEntity, new()
    {
        Response? response = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                if (tableName != null)
                {
                    tableClient = new TableClient(connectionString, tableName);
                }
                else
                {
                    CheckTableClient();
                }

                response = await tableClient!.UpsertEntityAsync(entity);
            });

            return response != null && response.Status == 204;
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTable(string tableName)
    {
        bool result = true;

        // Optional safety check to prevent accidental deletion of non-test tables.
        //if (!tableName.Contains("myTable", StringComparison.CurrentCultureIgnoreCase))
        //{
        //    return false;
        //}

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                var serviceClient = new TableServiceClient(connectionString);
                await serviceClient.DeleteTableAsync(tableName);
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
            result = false;
        }

        return result;
    }

    private void CheckIfTableExists(string tableName)
    {
        var serviceClient = new TableServiceClient(connectionString);
        serviceClient.CreateTableIfNotExists(tableName);
    }

    private void CheckTableClient()
    {
        if (tableClient == null)
        {
            throw new InvalidOperationException(nullTableCientError);
        }
    }
}