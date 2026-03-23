using Microsoft.Azure.Cosmos;
using AzureProject.Models;

namespace AzureProject.Services.Database
{
    public class AzureCosmosDatabaseService : IAzureCosmosDatabaseService, IDisposable
    {
        public CosmosClient Client { get; }
        public string? DatabaseId { get; }

        private readonly string _europeanContainerName;
        private readonly string _americanContainerName;
        private readonly string _asianContainerName;

        public AzureCosmosDatabaseService(IConfiguration configuration)
        {
            var section = configuration.GetSection("AzureCosmosDatabase");
            var uri = section.GetValue<string>("URI");
            var key = section.GetValue<string>("AcountKey");

            DatabaseId = section.GetValue<string>("Database");

            _europeanContainerName = section.GetValue<string>("EuropeanContainer") ?? "EuropeanCars";
            _americanContainerName = section.GetValue<string>("AmericanContainer") ?? "AmericanCars";
            _asianContainerName = section.GetValue<string>("AsianContainer") ?? "AsianCars";

            Client = new CosmosClient(uri, key);
            DatabaseResponse response = Client.CreateDatabaseIfNotExistsAsync(DatabaseId).GetAwaiter().GetResult();
        }


        //Prywatna metoda odpowiadająca za pobranie konkretnego kontenera na podstawie jego nazwy
        private Container GetContainerByName(string? containerName)
        {
            var container = containerName?.Trim();
            if (string.IsNullOrEmpty(container))
            {
                container = _europeanContainerName;
            }

            return Client.GetContainer(DatabaseId, container);
        }

        //Metoda odpowiadająca za stworzenie dokumentu samochodu i dodanie go
        //do wybranego aktualnie kontenera
        public async Task<Car> CreateCarAsync(Car car, string? containerName = null)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (string.IsNullOrWhiteSpace(car.Id))
            {
                car.Id = Guid.NewGuid().ToString();
            }

            var container = GetContainerByName(containerName);
            await container.CreateItemAsync(car, new PartitionKey(car.Id));
            return car;
        }

        //Metoda odpowiadająca za sczytanie wszystkich samochodów w danym kontenerze
        public async Task<IEnumerable<Car>> GetCarsAsync(string? containerName = null)
        {
            var container = GetContainerByName(containerName);
            var query = container.GetItemQueryIterator<Car>("SELECT * FROM c");
            var results = new List<Car>();
            while (query.HasMoreResults)
            {
                var resp = await query.ReadNextAsync();
                results.AddRange(resp.Resource);
            }
            return results;
        }

        //Metoda służąca do odczytania pojedynczego samochodu z danego kontenera
        //(powiązana z przyciskiem "Wczytaj" koło każdego samochodu w kontenerze)
        public async Task<Car?> GetCarAsync(string id, string? containerName = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var container = GetContainerByName(containerName);
            try
            {
                var response = await container.ReadItemAsync<Car>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        //Metoda służąca za zaktualizowanie danych, które zostały zmienione dla aktualnie
        //wczytanego dokumentu-samochodu
        public async Task<Car> UpdateCarAsync(Car car, string? containerName = null)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            if (string.IsNullOrWhiteSpace(car.Id)) throw new ArgumentException("Car must have Id to update", nameof(car));

            var container = GetContainerByName(containerName);
            var response = await container.UpsertItemAsync(car, new PartitionKey(car.Id));
            return response.Resource;
        }

        //Metoda odpowiadająca za usuwanie aktualnie wybranego dokumentu-samochodu z bazy
        public async Task DeleteCarAsync(string id, string? containerName = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            var container = GetContainerByName(containerName);
            await container.DeleteItemAsync<Car>(id, new PartitionKey(id));
        }
        
        //Definicja metody z interfejsu "IDisposable" do zwolnienia wszystkich
        //niezarządzanych zasobów trzymanych przez "CosmosClient" po zakończeniu pracy serwisu
        public void Dispose() => Client?.Dispose();
    }
}
