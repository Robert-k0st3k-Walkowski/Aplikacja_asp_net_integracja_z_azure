using AzureProject.Models;

namespace AzureProject.Services.AI
{
    //Interfejs odpowiadający za usługę "Azure Computer Vision"
    public interface IAzureComputerVisionService
    {
        Task<CarAnalysisResult?> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken = default);
    }
}
