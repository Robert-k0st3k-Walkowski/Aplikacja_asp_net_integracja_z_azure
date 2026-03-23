using Microsoft.Azure.Cosmos;
using AzureProject.Models;

namespace AzureProject.Services.Database
{
    //Interfejs odpowiadający za usługę "Azure CosmosDB"
    public interface IAzureCosmosDatabaseService
    {
        Task<Car> CreateCarAsync(Car car, string? containerName = null);
        Task<IEnumerable<Car>> GetCarsAsync(string? containerName = null);
        Task<Car?> GetCarAsync(string id, string? containerName = null);
        Task<Car> UpdateCarAsync(Car car, string? containerName = null);
        Task DeleteCarAsync(string id, string? containerName = null);
    }
}
