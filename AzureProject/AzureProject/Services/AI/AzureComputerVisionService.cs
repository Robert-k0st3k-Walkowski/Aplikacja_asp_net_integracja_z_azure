using AzureProject.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace AzureProject.Services.AI
{
    public class AzureComputerVisionService : IAzureComputerVisionService
    {
        private readonly string _subscriptionKey;
        private readonly string _endpoint;
        private readonly ComputerVisionClient _client;

        public AzureComputerVisionService(IConfiguration configuration)
        {
            _subscriptionKey = configuration["AzureComputerVision:SubscriptionKey"] ?? string.Empty;
            _endpoint = configuration["AzureComputerVision:Endpoint"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_subscriptionKey) || string.IsNullOrWhiteSpace(_endpoint))
            {
                throw new ArgumentNullException("AzureComputerVision configuration is missing.");
            }

            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            {
                Endpoint = _endpoint
            };
        }


        //Wyczytuje dane z obrazka załadowanego przy dodawaniu i uzupełnia te dane,
        //które wyszukał
        public async Task<CarAnalysisResult?> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            var features = new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Color,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Description
            };

            ImageAnalysis analysis;
            try
            {
                analysis = await _client.AnalyzeImageAsync(imageUrl, features);
            }
            catch
            {
                return null;
            }

            var result = new CarAnalysisResult
            {
                Extra = new Dictionary<string, string>()
            };

            try
            {
                var dominant = analysis.Color?.DominantColors?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(dominant))
                {
                    result.Kolor = dominant;
                }
            }
            catch
            {

            }

            try
            {
                if (analysis.Brands != null && analysis.Brands.Any())
                {
                    var top = analysis.Brands.OrderByDescending(b => b.Confidence).First();
                    result.Marka = top.Name;
                }
            }
            catch
            {

            }

            try
            {
                var bodyType = InferBodyType(analysis);
                if (!string.IsNullOrWhiteSpace(bodyType))
                {
                    result.RodzajNadwozia = bodyType;
                }
            }
            catch
            {

            }

            try
            {
                var modelCandidate = analysis.Tags
                    ?.Where(t => t.Name != null && (t.Name.Contains("model") || t.Name.Contains("series") || t.Name.Contains("sedan") || t.Name.Contains("suv")))
                    ?.OrderByDescending(t => t.Confidence)
                    ?.Select(t => t.Name)
                    ?.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(modelCandidate))
                {
                    result.Extra!["TagHint"] = modelCandidate;
                }

                var caption = analysis.Description?.Captions?.OrderByDescending(c => c.Confidence).FirstOrDefault()?.Text;
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    result.Extra!["Caption"] = caption;
                }
            }
            catch { }

            return result;
        }

        //Prywatna metoda pomocnicza pozwalająca zanalizować, jaki "rodzaj nadwozia"
        //dane auto załadowane jako zdjęcie posiada
        private static string? InferBodyType(ImageAnalysis analysis)
        {
            var candidates = new List<(string Tag, string Body, double Confidence)>();

            if (analysis.Tags != null)
            {
                foreach (var t in analysis.Tags)
                {
                    var name = t.Name?.ToLowerInvariant() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    if (name.Contains("suv")) candidates.Add((name, "SUV", t.Confidence));
                    else if (name.Contains("sedan")) candidates.Add((name, "Sedan", t.Confidence));
                    else if (name.Contains("hatchback")) candidates.Add((name, "Hatchback", t.Confidence));
                    else if (name.Contains("convertible") || name.Contains("cabriolet") || name.Contains("roadster")) candidates.Add((name, "Kabriolet", t.Confidence));
                    else if (name.Contains("wagon") || name.Contains("estate")) candidates.Add((name, "Kombi", t.Confidence));
                    else if (name.Contains("van") || name.Contains("minivan")) candidates.Add((name, "Van", t.Confidence));
                    else if (name.Contains("coupe") || name.Contains("coupé")) candidates.Add((name, "Coupe", t.Confidence));
                    else if (name.Contains("pickup")) candidates.Add((name, "SUV", t.Confidence));
                    else if (name.Contains("sports car")) candidates.Add((name, "Coupe", t.Confidence));
                }
            }

            if (!candidates.Any())
            {
                var caption = analysis.Description?.Captions?.OrderByDescending(c => c.Confidence).FirstOrDefault()?.Text?.ToLowerInvariant() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    if (caption.Contains("suv")) return "SUV";
                    if (caption.Contains("sedan")) return "Sedan";
                    if (caption.Contains("hatchback")) return "Hatchback";
                    if (caption.Contains("convertible") || caption.Contains("cabriolet") || caption.Contains("roadster")) return "Kabriolet";
                    if (caption.Contains("wagon") || caption.Contains("estate")) return "Kombi";
                    if (caption.Contains("van") || caption.Contains("minivan")) return "Van";
                    if (caption.Contains("coupe") || caption.Contains("coupé")) return "Coupe";
                    if (caption.Contains("pickup")) return "SUV";
                    if (caption.Contains("sports car")) return "Coupe";
                }
            }

            var best = candidates.OrderByDescending(c => c.Confidence).FirstOrDefault();
            return string.IsNullOrWhiteSpace(best.Body) ? null : best.Body;
        }
    }
}
