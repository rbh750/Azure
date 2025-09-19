using Azure.Data.Tables;

namespace Service.Azure.Storage.Table
{
    /// <summary>
    /// Provides operations for interacting with Azure Table Storage, including CRUD operations,
    /// query filtering, and optimistic concurrency control.
    /// </summary>
    public interface ITableStorageService
    {
        /// <summary>
        /// Gets the last operation error that occurred during a table operation.
        /// </summary>
        Exception OperationError { get; }
        
        /// <summary>
        /// Adds a new record to the table.
        /// 
        /// Example usage:
        /// <code>
        /// var entity = new MyEntity 
        /// { 
        ///     PartitionKey = "partition1", 
        ///     RowKey = "row1", 
        ///     Name = "John Doe" 
        /// };
        /// bool success = await tableService.AddRecord("MyTable", entity);
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of entity that implements ITableEntity</typeparam>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="entity">The entity to add to the table</param>
        /// <returns>True if the add operation succeeded, false otherwise</returns>
        Task<bool> AddRecord<T>(string? tableName, T entity) where T : class, ITableEntity, new();
        
        /// <summary>
        /// Configures the retry policy for table storage operations.
        /// Sets how many times to retry and how long to wait between retries for table operations.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delayMilliseconds">Initial delay between retries in milliseconds</param>
        /// <param name="maxDelayMilliseconds">Maximum delay between retries in milliseconds</param>
        void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);
        
        /// <summary>
        /// Counts how many records in the table match the given filter.
        /// 
        /// Example filter:
        /// <code>
        /// string filter = "PartitionKey eq 'users' and Age gt 18";
        /// int count = await tableService.CountRecords("MyTable", filter);
        /// </code>
        /// </summary>
        /// <param name="tableName">Name of the table to query</param>
        /// <param name="filter">OData filter expression to match records</param>
        /// <returns>The number of matching records</returns>
        Task<int> CountRecords(string tableName, string filter);
        
        /// <summary>
        /// Deletes a record from the table using its partition key and row key.
        /// </summary>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="partitionKey">The partition key of the record to delete</param>
        /// <param name="rowKey">The row key of the record to delete</param>
        /// <returns>True if the delete operation succeeded, false otherwise</returns>
        Task<bool> DeleteRecord(string? tableName, string partitionKey, string rowKey);
        
        /// <summary>
        /// Gets a single record from the table using its partition key and row key.
        /// </summary>
        /// <typeparam name="T">The type of entity that implements ITableEntity</typeparam>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="partitionKey">The partition key of the record to retrieve</param>
        /// <param name="rowKey">The row key of the record to retrieve</param>
        /// <returns>The record if found, null otherwise</returns>
        Task<T?> GetRecord<T>(string? tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();
        
        /// <summary>
        /// Gets multiple records from the table that match the given filter, up to the maximum number per page.
        /// 
        /// Example usage:
        /// <code>
        /// string filter = "PartitionKey eq 'orders' and Status eq 'pending'";
        /// var orders = await tableService.GetRecords&lt;OrderEntity&gt;("OrderTable", filter, 100);
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of entity that implements ITableEntity</typeparam>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="filter">OData filter expression to match records (required)</param>
        /// <param name="maxRecordsPerPage">Maximum number of records to return (max: 1000)</param>
        /// <returns>A list of matching records</returns>
        /// <exception cref="InvalidOperationException">Thrown when maxRecordsPerPage exceeds 1000 or filter is empty</exception>
        Task<List<T>> GetRecords<T>(string? tableName, string filter, int maxRecordsPerPage) where T : class, ITableEntity, new();
        
        /// <summary>
        /// Sets up the table client to work with a specific table, optionally creating it if it doesn't exist.
        /// This must be called before using other methods that don't specify a table name.
        /// </summary>
        /// <param name="tableName">Name of the table to initialize the client for</param>
        /// <param name="checkTable">If true, creates the table if it doesn't exist</param>
        void InitializeTableClient(string tableName, bool checkTable = false);
        
        /// <summary>
        /// Updates specific properties of an existing record in the table with safe concurrency control.
        /// Uses optimistic concurrency by default to prevent conflicts when multiple processes update the same record.
        /// 
        /// Example usage:
        /// <code>
        /// var updates = new Dictionary&lt;string, object?&gt;
        /// {
        ///     { "Status", "completed" },
        ///     { "UpdatedDate", DateTime.UtcNow }
        /// };
        /// bool success = await tableService.UpdateRecord("MyTable", "partition1", "row1", updates);
        /// </code>
        /// </summary>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="partitionKey">The partition key of the record to update</param>
        /// <param name="rowKey">The row key of the record to update</param>
        /// <param name="propertiesAndValues">Dictionary of property names and their new values</param>
        /// <param name="useOptimisticConcurrency">If true, uses ETag for concurrency control; if false, bypasses concurrency checks</param>
        /// <returns>True if the update operation succeeded, false otherwise</returns>
        Task<bool> UpdateRecord(string? tableName, string partitionKey, string rowKey, Dictionary<string, object?> propertiesAndValues, bool useOptimisticConcurrency = true);
        
        /// <summary>
        /// Adds a new record or updates an existing record in the table.
        /// If a record with the same partition key and row key exists, it will be replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity that implements ITableEntity</typeparam>
        /// <param name="tableName">Name of the table, or null to use the initialized table client</param>
        /// <param name="entity">The entity to add or update in the table</param>
        /// <returns>True if the upsert operation succeeded, false otherwise</returns>
        Task<bool> UpsertRecord<T>(string? tableName, T entity) where T : class, ITableEntity, new();

        /// <summary>
        /// Deletes an entire table (only allowed for test tables containing 'xunit' in the name).
        /// This is a safety measure to prevent accidental deletion of production tables.
        /// </summary>
        /// <param name="tableName">Name of the table to delete (must contain 'xunit')</param>
        /// <returns>True if the delete operation succeeded, false otherwise</returns>
        Task<bool> RemoveTable(string tableName);
    }
}