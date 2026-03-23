namespace AzureProject.Models
{
    //Jest to klasa w modelu przechowuj¹ca dane wyczytane ze zdjêcia
    public class CarAnalysisResult
    {
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public string? Kolor { get; set; }
        public string? RodzajNadwozia { get; set; }
        public Dictionary<string, string>? Extra { get; set; }
    }
}
