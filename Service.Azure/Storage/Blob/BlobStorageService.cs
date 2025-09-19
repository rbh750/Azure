using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Service.Azure.RetryPolicy;

namespace Service.Azure.Storage.Blob;

public class BlobStorageService(
    IOptions<StorageSettings> storageSettings,
    IRetryPolicyService retryPolicyService) : IBlobStorageService
{
    private readonly string connectionString = storageSettings.Value.ConnectionString;
    private readonly IRetryPolicyService retryPolicyService = retryPolicyService;
    private BlobContainerClient? blobContainer;

    public string? ResponseContentType { get; private set; }
    public Exception OperationError { get; private set; } = default!;

    /// <inheritdoc />
    public void ConfigureRetryPolicy(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
    {
        retryPolicyService.Configure(maxRetries, delayMilliseconds, maxDelayMilliseconds);
    }

    /// <inheritdoc />
    public void SetContainer(string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        blobContainer = blobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <inheritdoc />
    public BlobServiceClient GetBlobServiceClient()
    {
        return new BlobServiceClient(connectionString);
    }

    /// <inheritdoc />
    public async Task<bool> CopyBlob(string trgContainerName, string srcBlobName, string trgBlobName)
    {
        bool result = false;

        // Target.
        var trgBlobServiceClient = new BlobServiceClient(connectionString);
        var trgBlobContainer = trgBlobServiceClient.GetBlobContainerClient(trgContainerName);
        var trgBlobClient = trgBlobContainer.GetBlobClient(trgBlobName);

        // Source.
        var blobClient = GetBlobClientForBlob(srcBlobName);

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                var copyResult = await trgBlobClient.StartCopyFromUriAsync(blobClient.Uri);
                result = copyResult.HasCompleted;
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBlob(string blobName)
    {
        var blobClient = GetBlobClientForBlob(blobName);
        Response? response = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                response = await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots);
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return false;
        }

        return response is { Status: 202 };
    }

    /// <inheritdoc />
    public async Task<int> DeleteBlobsCreatedInLastSecondsAsync(string containerName, int secondsAgo, int maxItems = 1000)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        if (!await containerClient.ExistsAsync()) return 0;

        var cutoffTimeUtc = DateTimeOffset.UtcNow.AddSeconds(-secondsAgo).ToString("o");
        var tagExpression = $"createdUtc < '{cutoffTimeUtc}'";

        int deletedCount = 0;
        int count = 0;

        await foreach (var blobItem in containerClient.FindBlobsByTagsAsync(tagExpression))
        {
            if (count >= maxItems)
                break;

            var blobClient = containerClient.GetBlobClient(blobItem.BlobName);

            try
            {
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                deletedCount++;
            }
            catch (Exception ex)
            {
                OperationError = ex;
            }

            count++;
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public Uri GetBlobUri(string blobName)
    {
        var blobClient = GetBlobClientForBlob(blobName);
        return blobClient.Uri;
    }

    /// <inheritdoc />
    public long? GetBlobSize(string blobName)
    {
        try
        {
            var blobClient = GetBlobClientForBlob(blobName);
            return blobClient.GetProperties().Value.ContentLength;
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return null;
        }
    }

    /// <inheritdoc />
    public Uri? GetBlobSasUri(string blobName, DateTimeOffset expiration, bool grantAllPermissions = false)
    {
        try
        {
            var blobClient = GetBlobClientForBlob(blobName);

            BlobSasPermissions permissions;
            if (grantAllPermissions)
            {
                // For upload scenarios, we need write, create, delete and tag permissions.
                // For Azure Speech Service, we need read/list/add permissions.
                // Grant all necessary permissions for both scenarios
                permissions = BlobSasPermissions.Read | BlobSasPermissions.List | BlobSasPermissions.Add
                    | BlobSasPermissions.Write | BlobSasPermissions.Create | BlobSasPermissions.Delete | BlobSasPermissions.Tag;
            }
            else
            {
                // Default behavior: just read permission for the blob
                permissions = BlobSasPermissions.Read;
            }

            // Use BlobSasBuilder to create SAS with compatible API version
            return blobClient.GenerateSasUri(permissions, expiration);
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return null;
        }
    }

    /// <inheritdoc />
    public Uri? GetContainerSasUri(string containerName, DateTimeOffset expiration)
    {
        try
        {
            var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);

            // For Azure Speech Service destination, we need Write and List permissions.
            var permissions = BlobContainerSasPermissions.Write | BlobContainerSasPermissions.List;

            return containerClient.GenerateSasUri(permissions, expiration);
        }
        catch (Exception ex)
        {
            OperationError = ex;
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MemoryStream> GetStreamFromBlob(string blobName)
    {
        MemoryStream ms = new();
        var blobClient = GetBlobClientForBlob(blobName);
        BlobDownloadInfo? downloadInfo = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                downloadInfo = await blobClient.DownloadAsync();
                ResponseContentType = downloadInfo.ContentType;
                await downloadInfo.Content.CopyToAsync(ms);
                ms.Position = 0;
            });
        }
        catch (Exception ex)
        {
            OperationError = ex;
            ms.Dispose();
        }

        return ms;
    }

    /// <inheritdoc />
    public async Task<List<string>> ListBlobsInFolderAsync(string folderPrefix)
    {
        ArgumentNullException.ThrowIfNull(blobContainer);

        var blobNames = new List<string>();

        await foreach (var blobItem in blobContainer.GetBlobsAsync(prefix: folderPrefix))
        {
            blobNames.Add(blobItem.Name);
        }

        return blobNames;
    }

    /// <inheritdoc />
    public async Task<bool> PostBlob(string blobName, MemoryStream stream, bool tagWithTimestamp = false)
    {
        var blobClient = GetBlobClientForBlob(blobName);
        Response? response = null;

        try
        {
            await retryPolicyService.RunAsync(async () =>
            {
                response = (await blobClient.UploadAsync(stream, overwrite: true)).GetRawResponse();
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

    private BlobClient GetBlobClientForBlob(string blobName)
    {
        if (blobContainer != null)
        {
            var blobClient = blobContainer.GetBlobClient(blobName);
            return blobClient;
        }
        else
        {
            throw new InvalidOperationException("Blob container is null");
        }
    }
}