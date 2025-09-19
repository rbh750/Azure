using Service.Azure.Storage.Blob;
using System.Text;
using Xunit.Abstractions;

namespace Tests.Storage.Blob
{
    public class BlobStorageIntegrationTests(IBlobStorageService blobStorageService, IBlobStoragePushService blobStoragePushService, ITestOutputHelper output)
    {
        private const string ContainerName = "integration-tests"; // Set to a valid container name
        private const string BlobName = "integration-test-blob.txt";
        private const string BlobContent = "Hello from BlobStorage integration test!";

        [Fact]
        public async Task UploadAndDownloadBlob_WorksWithBothServices()
        {
            // Set the container for BlobStorageService
            blobStorageService.SetContainer(ContainerName);

            // Prepare the content to upload
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(BlobContent));

            // Generate a SAS URI for upload
            var sasUri = blobStorageService.GetBlobSasUri(BlobName, DateTimeOffset.UtcNow.AddMinutes(10), true);
            Assert.NotNull(sasUri);
            output.WriteLine($"Generated SAS URI: {sasUri.AbsoluteUri}");

            // Upload using BlobStoragePushService
            var uploadSuccess = await blobStoragePushService.PostBlobUsingSas(sasUri!, stream, true);
            output.WriteLine($"Upload success: {uploadSuccess}");
            Assert.True(uploadSuccess);

            // Download using BlobStorageService
            var downloadedStream = await blobStorageService.GetStreamFromBlob(BlobName);
            var downloadedContent = Encoding.UTF8.GetString(downloadedStream.ToArray());
            output.WriteLine($"Downloaded content: {downloadedContent}");
            Assert.Equal(BlobContent, downloadedContent);

            // Clean up: delete the blob
            var deleteSuccess = await blobStorageService.DeleteBlob(BlobName);
            output.WriteLine($"Delete success: {deleteSuccess}");
            Assert.True(deleteSuccess);
        }
    }
}
