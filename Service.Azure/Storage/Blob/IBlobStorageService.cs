using Azure.Storage.Blobs;

namespace Service.Azure.Storage.Blob
{
    /// <summary>
    /// Provides operations for interacting with Azure Blob Storage including upload, download,
    /// deletion, and SAS token generation for secure access.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Gets the last operation error that occurred during a blob operation.
        /// </summary>
        Exception OperationError { get; }
        
        /// <summary>
        /// Gets the content type of the last response from a blob operation.
        /// </summary>
        string? ResponseContentType { get; }

        /// <summary>
        /// Configures the retry policy for blob storage operations.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delayMilliseconds">Initial delay between retries in milliseconds</param>
        /// <param name="maxDelayMilliseconds">Maximum delay between retries in milliseconds</param>
        void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds);
        
        /// <summary>
        /// Copies a blob from one location to another within the storage account.
        /// </summary>
        /// <param name="trgContainerName">Target container name</param>
        /// <param name="srcBlobName">Source blob name (including path)</param>
        /// <param name="trgBlobName">Target blob name (including path)</param>
        /// <returns>True if the copy operation was successful, false otherwise</returns>
        Task<bool> CopyBlob(string trgContainerName, string srcBlobName, string trgBlobName);
        
        /// <summary>
        /// Deletes a blob from the current container.
        /// </summary>
        /// <param name="blobName">Name of the blob to delete (including path)</param>
        /// <returns>True if the deletion was successful, false otherwise</returns>
        Task<bool> DeleteBlob(string blobName);
        
        /// <summary>
        /// Deletes blobs that were created within the specified time window.
        /// Useful for cleanup operations and removing temporary files.
        /// </summary>
        /// <param name="containerName">Name of the container to clean up</param>
        /// <param name="secondsAgo">Delete blobs created within this many seconds ago</param>
        /// <param name="maxItems">Maximum number of blobs to delete (default: 1000)</param>
        /// <returns>Number of blobs that were deleted</returns>
        Task<int> DeleteBlobsCreatedInLastSecondsAsync(string containerName, int secondsAgo, int maxItems = 1000);
        
        /// <summary>
        /// Generates a SAS (Shared Access Signature) URI for secure access to a specific blob.
        /// 
        /// Example usage:
        /// <code>
        /// var sasUri = blobService.GetBlobSasUri("myfile.pdf", DateTimeOffset.UtcNow.AddHours(1), true);
        /// // Share this URI to allow temporary access to the blob
        /// </code>
        /// </summary>
        /// <param name="blobName">Name of the blob (including path)</param>
        /// <param name="expiration">When the SAS token should expire</param>
        /// <param name="grantAllPermissions">If true, grants read/write/delete permissions; if false, read-only</param>
        /// <returns>SAS URI for the blob, or null if generation failed</returns>
        Uri? GetBlobSasUri(string blobName, DateTimeOffset expiration, bool grantAllPermissions = false);
        
        /// <summary>
        /// Generates a SAS URI for accessing an entire container.
        /// </summary>
        /// <param name="containerName">Name of the container</param>
        /// <param name="expiration">When the SAS token should expire</param>
        /// <returns>SAS URI for the container, or null if generation failed</returns>
        Uri? GetContainerSasUri(string containerName, DateTimeOffset expiration);
        
        /// <summary>
        /// Gets the size of a blob in bytes.
        /// </summary>
        /// <param name="blobName">Name of the blob (including path)</param>
        /// <returns>Size in bytes, or null if the blob doesn't exist</returns>
        long? GetBlobSize(string blobName);
        
        /// <summary>
        /// Gets the public URI of a blob (without SAS token).
        /// </summary>
        /// <param name="blobName">Name of the blob (including path)</param>
        /// <returns>Public URI of the blob</returns>
        Uri GetBlobUri(string blobName);
        
        /// <summary>
        /// Downloads a blob content as a memory stream.
        /// </summary>
        /// <param name="blobName">Name of the blob to download (including path)</param>
        /// <returns>MemoryStream containing the blob content</returns>
        Task<MemoryStream> GetStreamFromBlob(string blobName);
        
        /// <summary>
        /// Lists all blobs within a specific folder/prefix.
        /// </summary>
        /// <param name="folderPrefix">Folder path prefix to search within</param>
        /// <returns>List of blob names matching the prefix</returns>
        Task<List<string>> ListBlobsInFolderAsync(string folderPrefix);
        
        /// <summary>
        /// Uploads a file stream as a blob to the storage container.
        /// </summary>
        /// <param name="blobName">Name for the blob (including path)</param>
        /// <param name="fileStream">Memory stream containing the file data</param>
        /// <param name="tagWithTimestamp">If true, adds timestamp metadata to the blob</param>
        /// <returns>True if the upload was successful, false otherwise</returns>
        Task<bool> PostBlob(string blobName, MemoryStream fileStream, bool tagWithTimestamp = false);
        
        /// <summary>
        /// Sets the current working container for subsequent operations.
        /// </summary>
        /// <param name="containerName">Name of the container to use</param>
        void SetContainer(string containerName);
        
        /// <summary>
        /// Gets the underlying BlobServiceClient for advanced operations not covered by this interface.
        /// </summary>
        /// <returns>The BlobServiceClient instance for direct Azure SDK access</returns>
        BlobServiceClient GetBlobServiceClient();
    }
}