using AzureProject.Models;

namespace AzureProject.Services.Storage
{
    //Interfejs odpowiadający za usługę "Azure Blob Storage"
    public interface IAzureBlobStorageService
    {
        Task<List<BlobObject>> GetBlobsAsync();
        Task<BlobObject> UploadBlobAsync(IFormFile formFile);
        Task UploadMetadataAsnyc(BlobObject blobObject, Dictionary<string, string> metadata);
        Task DeleteAsync(string blobName);
    }
}