using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;

namespace ApiBougies.Services
{
    public class ServiceStorageBlob
    {
        private BlobServiceClient client;

        public ServiceStorageBlob(BlobServiceClient client)
        {
            this.client = client;
        }
        public string GetContainerUrl(string containerName)
        {
            BlobContainerClient containerClient = this.client.GetBlobContainerClient(containerName);

            return containerClient.Uri.AbsoluteUri;
        }

        public async Task UploadBlobAsync(string containerName, string blobName, Stream stream)
        {
            BlobContainerClient containerClient = this.client.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            await containerClient.UploadBlobAsync(blobName, stream);
        }

    }
}


//para mostrar la imagen del blob

