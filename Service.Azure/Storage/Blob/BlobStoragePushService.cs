using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Service.Azure.RetryPolicy;
using System.Text;

namespace Service.Azure.Storage.Blob;

public class BlobStoragePushService(IRetryPolicyService retryPolicyService) : IBlobStoragePushService
{
    public Exception OperationError { get; private set; } = default!;

    // Optimal block size for performance (4MB is recommended for most scenarios)
    private const int DefaultBlockSize = 4 * 1024 * 1024; // 4MB

    /// <inheritdoc />
    public async Task<bool> PostBlobUsingSas(Uri sasUri, MemoryStream stream, bool tagWithTimestamp = false)
    {
        Response? response = null;

        try
        {
            // Validate stream state before upload using ThrowIf methods
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentOutOfRangeException.ThrowIfZero(stream.Length, nameof(stream));

            if (!stream.CanRead) throw new InvalidOperationException("Stream is not readable");
            if (!stream.CanSeek) throw new InvalidOperationException("Stream is not seekable");

            var blobClient = new BlobClient(sasUri);

            await retryPolicyService.RunAsync(async () =>
            {
                // Reset position again in case retry policy runs multiple times
                stream.Position = 0;

                // Force block blob upload for better performance on larger files
                response = await UploadAsBlockBlob(blobClient, stream, DefaultBlockSize);
            });

            // Optionally tag with createdUtc timestamp after successful upload
            if (tagWithTimestamp && response is { Status: 201 })
            {
                var tags = new Dictionary<string, string>
                {
                    { "createdUtc", DateTime.UtcNow.ToString("o") }
                };

                await blobClient.SetTagsAsync(tags).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return false;
        }
        finally
        {
            if (stream != null) await stream.DisposeAsync().ConfigureAwait(false);
        }

        return response is { Status: 201 };
    }

    // Uploads a large stream as a block blob by splitting it into smaller blocks for better performance.
    // All blocks are uploaded in parallel and then committed to create the final blob.
    private static async Task<Response> UploadAsBlockBlob(BlobClient blobClient, MemoryStream stream, int blockSize)
    {
        // Create BlockBlobClient directly from the SAS URI
        var blockBlobClient = new BlockBlobClient(blobClient.Uri);

        var blockIds = new List<string>();
        var buffer = new byte[blockSize];
        var blockIndex = 0;

        // Stage all blocks in parallel for better performance
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount); // Limit concurrent uploads

        while (stream.Position < stream.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            // Generate a unique block ID (must be base64 encoded and same length for all blocks)
            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"block-{blockIndex:D10}"));
            blockIds.Add(blockId);

            // Create a copy of the buffer for this block to avoid race conditions
            var blockData = new byte[bytesRead];
            Array.Copy(buffer, blockData, bytesRead);

            // Stage the block in parallel
            var task = StageBlockAsync(blockBlobClient, blockId, blockData, semaphore);
            tasks.Add(task);

            blockIndex++;
        }

        // Wait for all blocks to be staged
        await Task.WhenAll(tasks);
        semaphore.Dispose();

        // Commit all blocks to create the blob
        var commitResponse = await blockBlobClient.CommitBlockListAsync(blockIds);
        return commitResponse.GetRawResponse();
    }

    // Uploads a single block of data to Azure Storage as a temporary block.
    // The block becomes part of the final blob only after CommitBlockListAsync is called.
    private static async Task StageBlockAsync(BlockBlobClient blockBlobClient, string blockId, byte[] blockData, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            using var blockStream = new MemoryStream(blockData);

            // StageBlockAsync uploads the block to Azure Blob Storage as a temporary block.
            // The block is not part of the final blob until CommitBlockListAsync is called.
            await blockBlobClient.StageBlockAsync(blockId, blockStream);
        }
        finally
        {
            semaphore.Release();
        }
    }
}