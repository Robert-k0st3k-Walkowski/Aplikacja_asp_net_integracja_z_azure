using AzureProject.Models;
using AzureProject.Services.AI;
using AzureProject.Services.Database;
using AzureProject.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace AzureProject.Controllers
{
    public class HomeController : Controller
    {
        //Deklaracja zmiennych do podpiêcia us³ug pod "Kontroler"

        private readonly ILogger<HomeController> _logger;
        private readonly IAzureCosmosDatabaseService _db;
        private readonly IAzureBlobStorageService _blobService;
        private readonly IAzureComputerVisionService _vision;

        //Konstruktor klasy "HomeController"
        public HomeController(ILogger<HomeController> logger, IAzureCosmosDatabaseService db, IAzureBlobStorageService blobService, IAzureComputerVisionService vision)
        {
            _logger = logger;
            _db = db;
            _blobService = blobService;
            _vision = vision;
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za aktualizowanie ca³ego widoku strony
        //(nie powoduje ¿adnych konrektnych zmian)
        public async Task<IActionResult> Index([FromQuery] string? container)
        {
            var readContainer = string.IsNullOrWhiteSpace(container) ? "EuropeanCars" : container;
            var cars = await _db.GetCarsAsync(readContainer);

            ViewBag.Cars = cars;
            ViewBag.ReadContainer = readContainer;
            ViewBag.EditContainer = readContainer;
            ViewBag.FormCar = new Car();
            ViewBag.StatusMessage = TempData["StatusMessage"] ?? string.Empty;

            if (TempData.ContainsKey("LastAnalysis"))
            {
                try
                {
                    var json = TempData["LastAnalysis"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var vm = JsonConvert.DeserializeObject<CarAnalysisResult>(json);
                        ViewBag.Analysis = vm;
                    }
                }
                catch { }
            }

            return View();
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za za³adowanie wybranego dokumentu z wybranego kontenera
        [HttpPost]
        public async Task<IActionResult> LoadSelected(string selectedId, string readContainer)
        {
            var container = string.IsNullOrWhiteSpace(readContainer) ? "EuropeanCars" : readContainer;
            var cars = await _db.GetCarsAsync(container);
            var car = string.IsNullOrWhiteSpace(selectedId) ? new Car() : await _db.GetCarAsync(selectedId, container);

            ViewBag.Cars = cars;
            ViewBag.ReadContainer = container;
            ViewBag.EditContainer = container;
            ViewBag.FormCar = car ?? new Car();

            if (TempData.ContainsKey("LastAnalysis"))
            {
                try
                {
                    var json = TempData["LastAnalysis"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var vm = JsonConvert.DeserializeObject<CarAnalysisResult>(json);
                        ViewBag.Analysis = vm;
                    }
                }
                catch { }
            }

            return View("Index");
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za stworzenie dokumentu typu "Car" na podstawie
        //podanych w³asnoœci i dodanie go do bazy danych do wybranego kontenera
        [HttpPost]
        public async Task<IActionResult> Create(Car formCar, string editContainer, IFormFile? imageFile)
        {
            var container = string.IsNullOrWhiteSpace(editContainer) ? "EuropeanCars" : editContainer;

            if (formCar == null) return RedirectToAction(nameof(Index), new { container });

            if (string.IsNullOrWhiteSpace(formCar.Id))
            {
                formCar.Id = Guid.NewGuid().ToString();
            }

            CarAnalysisResult? analysisVm = null;

            if ((imageFile != null && imageFile.Length > 0) && string.IsNullOrWhiteSpace(formCar.ImageUri))
            {
                var blob = await _blobService.UploadBlobAsync(imageFile);

                formCar.ImageFilename = blob.Name ?? string.Empty;
                formCar.ImageUri = blob.ImageUri ?? string.Empty;

                try
                {
                    var analysis = await _vision.AnalyzeImageAsync(formCar.ImageUri);
                    if (analysis != null)
                    {
                        analysisVm = new CarAnalysisResult
                        {
                            Marka = analysis.Marka,
                            Model = analysis.Model,
                            Kolor = analysis.Kolor,
                            RodzajNadwozia = analysis.RodzajNadwozia,
                            Extra = analysis.Extra
                        };

                        if (string.IsNullOrWhiteSpace(formCar.Marka) && !string.IsNullOrWhiteSpace(analysis.Marka))
                            formCar.Marka = analysis.Marka;
                        if (string.IsNullOrWhiteSpace(formCar.Model) && !string.IsNullOrWhiteSpace(analysis.Model))
                            formCar.Model = analysis.Model;
                        if (string.IsNullOrWhiteSpace(formCar.Kolor) && !string.IsNullOrWhiteSpace(analysis.Kolor))
                            formCar.Kolor = analysis.Kolor;
                        if (string.IsNullOrWhiteSpace(formCar.RodzajNadwozia) && !string.IsNullOrWhiteSpace(analysis.RodzajNadwozia))
                            formCar.RodzajNadwozia = analysis.RodzajNadwozia;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Computer Vision analysis failed for blob {Blob}", blob.Name);
                }

                var metadata = new Dictionary<string, string> { { "carId", formCar.Id } };
                await _blobService.UploadMetadataAsnyc(blob, metadata);
            }
            else if (!string.IsNullOrWhiteSpace(formCar.ImageFilename))
            {
                try
                {
                    var metadata = new Dictionary<string, string> { { "carId", formCar.Id } };
                    await _blobService.UploadMetadataAsnyc(new BlobObject { Name = formCar.ImageFilename, ImageUri = formCar.ImageUri }, metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to attach metadata to existing blob {Blob}", formCar.ImageFilename);
                }
            }

            await _db.CreateCarAsync(formCar, container);
            TempData["StatusMessage"] = "Dokument dodany";

            if (analysisVm != null)
            {
                TempData["LastAnalysis"] = JsonConvert.SerializeObject(analysisVm);
            }

            return RedirectToAction(nameof(Index), new { container });
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za zaktualizowanie wybranego ówczeœnie dokumentu
        //o nowo podane dane
        [HttpPost]
        public async Task<IActionResult> Update(Car formCar, string editContainer, IFormFile? imageFile)
        {
            var container = string.IsNullOrWhiteSpace(editContainer) ? "EuropeanCars" : editContainer;

            if (formCar is null || string.IsNullOrWhiteSpace(formCar.Id))
            {
                TempData["StatusMessage"] = "Brak Id do uaktualnienia";
                return RedirectToAction(nameof(Index), new { container });
            }

            var existing = await _db.GetCarAsync(formCar.Id, container);

            if (existing != null &&
                (existing.Marka != formCar.Marka ||
                existing.Model != formCar.Model ||
                existing.Rocznik != formCar.Rocznik ||
                existing.Kolor != formCar.Kolor ||
                existing.RodzajNadwozia != formCar.RodzajNadwozia))
            {
                existing = formCar;
            }

            CarAnalysisResult? analysisVm = null;

            if ((imageFile != null && imageFile.Length > 0) && string.IsNullOrWhiteSpace(formCar.ImageUri))
            {
                var blob = await _blobService.UploadBlobAsync(imageFile);

                formCar.ImageFilename = blob.Name ?? string.Empty;
                formCar.ImageUri = blob.ImageUri ?? string.Empty;

                try
                {
                    var analysis = await _vision.AnalyzeImageAsync(formCar.ImageUri);
                    if (analysis != null)
                    {
                        analysisVm = new CarAnalysisResult
                        {
                            Marka = analysis.Marka,
                            Model = analysis.Model,
                            Kolor = analysis.Kolor,
                            RodzajNadwozia = analysis.RodzajNadwozia,
                            Extra = analysis.Extra
                        };

                        if (string.IsNullOrWhiteSpace(formCar.Marka) && !string.IsNullOrWhiteSpace(analysis.Marka))
                            formCar.Marka = analysis.Marka;
                        if (string.IsNullOrWhiteSpace(formCar.Model) && !string.IsNullOrWhiteSpace(analysis.Model))
                            formCar.Model = analysis.Model;
                        if (string.IsNullOrWhiteSpace(formCar.Kolor) && !string.IsNullOrWhiteSpace(analysis.Kolor))
                            formCar.Kolor = analysis.Kolor;
                        if (string.IsNullOrWhiteSpace(formCar.RodzajNadwozia) && !string.IsNullOrWhiteSpace(analysis.RodzajNadwozia))
                            formCar.RodzajNadwozia = analysis.RodzajNadwozia;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Computer Vision analysis failed for blob {Blob}", blob.Name);
                }

                var metadata = new Dictionary<string, string> { { "carId", formCar.Id } };
                await _blobService.UploadMetadataAsnyc(blob, metadata);
            }
            else if (!string.IsNullOrWhiteSpace(formCar.ImageFilename))
            {
                try
                {
                    var metadata = new Dictionary<string, string> { { "carId", formCar.Id } };
                    await _blobService.UploadMetadataAsnyc(new BlobObject { Name = formCar.ImageFilename, ImageUri = formCar.ImageUri }, metadata);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to attach metadata to existing blob {Blob}", formCar.ImageFilename);
                }
            }

            await _db.UpdateCarAsync(formCar, container);
            TempData["StatusMessage"] = "Dokument uaktualniony";

            if (analysisVm != null)
            {
                TempData["LastAnalysis"] = JsonConvert.SerializeObject(analysisVm);
            }

            return RedirectToAction(nameof(Index), new { container });
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za usuwanie wybranego ówczeœnie dokumentu z
        //ca³ej bazy i konkretnego kontenera
        [HttpPost]
        public async Task<IActionResult> Delete(string selectedId, string readContainer)
        {
            var container = string.IsNullOrWhiteSpace(readContainer) ? "EuropeanCars" : readContainer;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                var car = await _db.GetCarAsync(selectedId, container);
                if (car != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(car.ImageFilename))
                        {
                            var blobName = car.ImageFilename;
                            if (Uri.IsWellFormedUriString(blobName, UriKind.Absolute))
                            {
                                blobName = Path.GetFileName(new Uri(blobName).LocalPath);
                            }

                            await _blobService.DeleteAsync(blobName);
                        }
                        else if (!string.IsNullOrWhiteSpace(car.ImageUri))
                        {
                            var blobName = Path.GetFileName(new Uri(car.ImageUri).LocalPath);
                            await _blobService.DeleteAsync(blobName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete blob for car {CarId}", selectedId);
                    }

                    await _db.DeleteCarAsync(selectedId, container);
                    TempData["StatusMessage"] = "Dokument usuniêty";
                }
                else
                {
                    TempData["StatusMessage"] = "Brak wybranego dokumentu do usuniêcia";
                }
            }
            else
            {
                TempData["StatusMessage"] = "Brak wybranego dokumentu do usuniêcia";
            }

            return RedirectToAction(nameof(Index), new { container });
        }

        //Zadanie asynchroniczne odpowiadaj¹ce za analizê obrazu
        //(zwi¹zana z us³ug¹ computer vision i auto-uzupe³nianiem)
        [HttpPost]
        public async Task<IActionResult> AnalyzeImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return Json(new { success = false, message = "Brak pliku" });
            }

            try
            {
                var blob = await _blobService.UploadBlobAsync(imageFile);
                var analysis = await _vision.AnalyzeImageAsync(blob.ImageUri ?? string.Empty);

                return Json(new
                {
                    success = true,
                    imageFilename = blob.Name,
                    imageUri = blob.ImageUri,
                    marka = analysis?.Marka,
                    model = analysis?.Model,
                    kolor = analysis?.Kolor,
                    rodzajNadwozia = analysis?.RodzajNadwozia,
                    extra = analysis?.Extra
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AnalyzeImage failed");
                return Json(new { success = false, message = "B³¹d analizy" });
            }
        }
    }
}