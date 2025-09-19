namespace Service.Azure.CosmosDb
{
    /// <summary>
    /// Provides operations for interacting with Azure Cosmos DB containers including CRUD operations,
    /// optimistic concurrency control, and automatic retry handling.
    /// </summary>
    public interface ICosmosDbService
    {
        /// <summary>
        /// Gets the last operation error that occurred during a database operation.
        /// </summary>
        Exception OperationError { get; }
        
        /// <summary>
        /// Configures the retry policy for database operations.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delayMilliseconds">Initial delay between retries in milliseconds</param>
        /// <param name="maxDelayMilliseconds">Maximum delay between retries in milliseconds</param>
        void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);
        
        /// <summary>
        /// Deletes a record from the specified container.
        /// </summary>
        /// <typeparam name="T">The type of entity to delete</typeparam>
        /// <param name="entity">The entity to delete (must contain partition key and id properties)</param>
        /// <param name="containerReference">Reference to the container configuration</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        Task<bool> Delete<T>(T entity, string containerReference) where T : new();
        
        /// <summary>
        /// Retrieves a single record from the container by partition key and record ID.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve</typeparam>
        /// <param name="containerReference">Reference to the container configuration</param>
        /// <param name="partitionKeyValue">The partition key value</param>
        /// <param name="recordId">The unique record identifier</param>
        /// <returns>The entity if found, null otherwise</returns>
        Task<T?> Get<T>(string containerReference, object partitionKeyValue, string recordId) where T : new();
        
        /// <summary>
        /// Retrieves multiple records from the container using a SQL query.
        /// 
        /// Example query:
        /// <code>
        /// string query = "SELECT * FROM c WHERE c.status = 'active' AND c.created > '2023-01-01'";
        /// var results = await cosmosService.GetAll&lt;MyEntity&gt;(query, "MyContainer");
        /// </code>
        /// </summary>
        /// <typeparam name="T">The type of entities to retrieve</typeparam>
        /// <param name="query">SQL query string for filtering records</param>
        /// <param name="containerReference">Reference to the container configuration</param>
        /// <returns>List of matching entities, or null if none found</returns>
        Task<List<T>?> GetAll<T>(string query, string containerReference) where T : new();
        
        /// <summary>
        /// Patches fields atomically using optimistic concurrency control with fresh data on each retry.
        /// All operations are atomic - fresh data is read on each retry attempt and values are applied with fresh ETag.
        /// Supports direct values and Func&lt;dynamic, object&gt; for calculations based on current data.
        /// 
        /// Example usage:
        /// <code>
        /// var fieldValues = new Dictionary&lt;string, object&gt;
        /// {
        ///     { "status", "completed" },
        ///     { "updatedDate", DateTime.UtcNow },
        ///     { "counter", new Func&lt;dynamic, object&gt;(current => current.counter + 1) }
        /// };
        /// bool success = await cosmosService.PatchAtomicAsync(fieldValues, "MyContainer", partitionKey, recordId);
        /// </code>
        /// </summary>
        /// <param name="fieldValues">Dictionary of field names and their new values or calculation functions</param>
        /// <param name="containerReference">Reference to the container configuration</param>
        /// <param name="partitionKeyValue">The partition key value</param>
        /// <param name="recordId">The unique record identifier</param>
        /// <returns>True if the patch operation was successful, false otherwise</returns>
        Task<bool> PatchAtomicAsync(Dictionary<string, object> fieldValues, string containerReference, object partitionKeyValue, string recordId);
        
        /// <summary>
        /// Creates a new record or updates an existing record in the container.
        /// </summary>
        /// <typeparam name="T">The type of entity to upsert</typeparam>
        /// <param name="entity">The entity to create or update</param>
        /// <param name="containerReference">Reference to the container configuration</param>
        /// <param name="timeToLive">Optional TTL in seconds for automatic expiration</param>
        /// <returns>True if the upsert operation was successful, false otherwise</returns>
        Task<bool> Upsert<T>(T entity, string containerReference, int? timeToLive = null) where T : new();
    }
}