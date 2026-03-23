using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using AzureProject.Models;

namespace AzureProject.Services.Storage
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            _storageConnectionString = configuration["AzureBlobStorage:ConnectionString"]
                                       ?? throw new ArgumentNullException("AzureBlobStorage:ConnectionString");
            _storageContainerName = configuration["AzureBlobStorage:ContainerName"]
                                    ?? throw new ArgumentNullException("AzureBlobStorage:ContainerName");

            var serviceClient = new BlobServiceClient(_storageConnectionString);
            _containerClient = serviceClient.GetBlobContainerClient(_storageContainerName);
        }

        //Metoda odpowiadająca za pobieranie blobów i zwracanie listy tych blobów
        public async Task<List<BlobObject>> GetBlobsAsync()
        {
            var results = new List<BlobObject>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var client = _containerClient.GetBlobClient(blobItem.Name);
                results.Add(new BlobObject
                {
                    Name = blobItem.Name,
                    ImageUri = client.Uri.ToString()
                });
            }
            return results;
        }

        //Metoda odpowiadająca za wrzucenie nowego bloba do usługi "Azure Blob Storage"
        public async Task<BlobObject> UploadBlobAsync(IFormFile formFile)
        {
            if (formFile == null) throw new ArgumentNullException(nameof(formFile));
            var ext = Path.GetExtension(formFile.FileName) ?? string.Empty;
            var blobName = $"{Guid.NewGuid()}{ext}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            using (var stream = formFile.OpenReadStream())
            {
                var headers = new BlobHttpHeaders
                {
                    ContentType = formFile.ContentType ?? "application/octet-stream"
                };
                await blobClient.UploadAsync(stream, headers);
            }

            return new BlobObject { Name = blobName, ImageUri = blobClient.Uri.ToString() };
        }

        //Metoda odpowiadająca za ustawianie metadanych dla danego bloba w "Azure Blob Storage"
        public async Task UploadMetadataAsnyc(BlobObject blobObject, Dictionary<string, string> metadata)
        {
            if (blobObject == null) throw new ArgumentNullException(nameof(blobObject));
            if (string.IsNullOrWhiteSpace(blobObject.Name)) throw new ArgumentException("BlobObject must have Name", nameof(blobObject));

            var blobClient = _containerClient.GetBlobClient(blobObject.Name);
            await blobClient.SetMetadataAsync(metadata);
        }

        //Metoda odpowiadająca za usuwanie bloba, w zależności od tego, który plik
        //samochodu chcemy usunąć z bazy.
        public async Task DeleteAsync(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName)) return;

            if (Uri.IsWellFormedUriString(blobName, UriKind.Absolute))
            {
                try
                {
                    blobName = Path.GetFileName(new Uri(blobName).LocalPath);
                }
                catch
                {

                }
            }

            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}