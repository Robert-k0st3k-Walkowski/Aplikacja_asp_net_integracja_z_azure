using Newtonsoft.Json;

namespace AzureProject.Models
{
    //Klasa modelu odpowiadająca za przechowywanie pojedynczego dokumentu
    public class Car
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("marka")]
        public string Marka { get; set; } = string.Empty;

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("kolor")]
        public string Kolor { get; set; } = string.Empty;

        [JsonProperty("rocznik")]
        public int Rocznik { get; set; }

        [JsonProperty("rodzaj nadwozia")]
        public string RodzajNadwozia { get; set; } = string.Empty;

        [JsonProperty("imageFilename")]
        public string ImageFilename { get; set; } = string.Empty;

        [JsonProperty("imageUri")]
        public string ImageUri { get; set; } = string.Empty;
    }
}