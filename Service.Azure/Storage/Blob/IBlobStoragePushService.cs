namespace Service.Azure.Storage.Blob;

/// <summary>
/// Provides operations for uploading blobs to Azure Storage using SAS URIs with optimized 
/// block-based uploads for improved performance and reliability.
/// </summary>
public interface IBlobStoragePushService
{
    /// <summary>
    /// Gets the last operation error that occurred during a blob upload operation.
    /// </summary>
    Exception OperationError { get; }
    
    /// <summary>
    /// Uploads a memory stream as a blob using a secure SAS URL with optimized block-based upload.
    /// For large files, the stream is automatically split into blocks and uploaded in parallel for better performance.
    /// 
    /// Example usage:
    /// <code>
    /// using var fileStream = new MemoryStream(fileBytes);
    /// var sasUri = new Uri("https://storage.blob.core.windows.net/container/blob?sv=2021-06-08&amp;se=...");
    /// bool success = await blobPushService.PostBlobUsingSas(sasUri, fileStream, true);
    /// if (success)
    /// {
    ///     Console.WriteLine("File uploaded successfully");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Upload failed: {blobPushService.OperationError?.Message}");
    /// }
    /// </code>
    /// </summary>
    /// <param name="sasUri">The SAS (Shared Access Signature) URI with write permissions to the blob storage</param>
    /// <param name="fileStream">The memory stream containing the file data to upload</param>
    /// <param name="tagWithTimestamp">If true, adds a 'createdUtc' tag with the current UTC timestamp to the blob metadata</param>
    /// <returns>True if the upload operation succeeded, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when sasUri or fileStream is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when fileStream is empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when fileStream is not readable or seekable</exception>
    Task<bool> PostBlobUsingSas(Uri sasUri, MemoryStream fileStream, bool tagWithTimestamp = false);
}