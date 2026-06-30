using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Domain.Models;
using ArtifactsAPI.Infrastructure.Persistence;
using System.Text.Json;

namespace ArtifactsAPI.Application.Services
{
    public class ScanRecordService : IScanRecordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;

        // Inject all required services (database, server environment, factory, and Http)
        public ScanRecordService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _env = env;
            _scopeFactory = scopeFactory;
            _httpClient = httpClientFactory.CreateClient();
        }

        // =========================================================================
        // 1️⃣ First method: Direct upload of 3D file (your original code is 100% correct)
        // =========================================================================
        /// <summary>
        /// Creates and uploads a 3D scan file directly to the server for an artifact.
        /// Validates the artifact exists and file is provided before uploading.
        /// Generates a unique filename and returns the file URL.
        /// </summary>
        /// <param name="dto">Data transfer object containing ArtifactId and ScanFile</param>
        /// <param name="baseUrl">The base URL for constructing the file access URL</param>
        /// <returns>Tuple with success status, error message if failed, and ScanRecord if successful</returns>
        public async Task<(bool IsSuccess, string ErrorMessage, ScanRecord Data)> CreateScanAsync(CreateScanRecordDto dto, string baseUrl)
        {
            var artifact = await _context.Artifacts.FindAsync(dto.ArtifactId);
            if (artifact == null)
                return (false, "Artifact not found.", null);

            if (dto.ScanFile == null || dto.ScanFile.Length == 0)
                return (false, "Please upload a valid 3D scan file.", null);

            try
            {
                // Use WebRootPath to save in the server's wwwroot folder
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "uploads", "Models");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + dto.ScanFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ScanFile.CopyToAsync(fileStream);
                }

                string fileUrl = $"{baseUrl}/uploads/Models/{uniqueFileName}";

                var newScan = new ScanRecord
                {
                    ArtifactId = dto.ArtifactId,
                    Date = DateTime.UtcNow,
                    ModelFileUrl = fileUrl
                };

                _context.ScanRecords.Add(newScan);
                await _context.SaveChangesAsync();

                return (true, null, newScan);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to upload the scan file. Error: {ex.Message}", null);
            }
        }

        // =========================================================================
        // 2️⃣ Second method: Receive 50 images from mobile app and send them to Python API
        // =========================================================================
        /// <summary>
        /// Receives multiple 3D scan images from the mobile app and processes them asynchronously.
        /// Temporarily saves images and triggers background job for 3D model generation via Python API.
        /// Returns a JobId for tracking the asynchronous processing status.
        /// </summary>
        /// <param name="dto">Data transfer object containing ArtifactId and collection of image files</param>
        /// <returns>Tuple with success status, error message if failed, and JobId for tracking if successful</returns>
        public async Task<(bool IsSuccess, string ErrorMessage, string JobId)> ProcessAndUploadScansAsync(Upload3DScanDto dto)
        {
            var artifact = await _context.Artifacts.FindAsync(dto.ArtifactId);
            if (artifact == null)
                return (false, "Artifact not found.", null);

            if (dto.Images == null || !dto.Images.Any())
                return (false, "Please upload the scan images.", null);

            try
            {
                string jobId = Guid.NewGuid().ToString();
                string tempFolder = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), "temp_scans", jobId);
                Directory.CreateDirectory(tempFolder);

                // Save images temporarily very quickly
                foreach (var file in dto.Images)
                {
                    if (file.Length > 0)
                    {
                        string filePath = Path.Combine(tempFolder, file.FileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                    }
                }

                // Trigger the background task so the mobile app doesn't wait
                _ = Task.Run(() => SendToPythonBackgroundAsync(jobId, tempFolder, dto.ArtifactId));

                return (true, null, jobId);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to process images. Error: {ex.Message}", null);
            }
        }

        // =========================================================================
        // ⚙️ Background job function to communicate with FastAPI and save the model URL
        // =========================================================================
        /// <summary>
        /// Background job that sends scan images to the Python API for 3D model generation.
        /// Runs asynchronously and saves the generated model URL to the database.
        /// Cleans up temporary image files after completion regardless of success or failure.
        /// </summary>
        /// <param name="jobId">Unique identifier for this processing job</param>
        /// <param name="folderPath">Path to the folder containing temporarily saved image files</param>
        /// <param name="artifactId">The ID of the artifact to associate with the generated 3D model</param>
        /// <returns>Awaitable task for tracking job completion</returns>
        private async Task SendToPythonBackgroundAsync(string jobId, string folderPath, int artifactId)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var files = Directory.GetFiles(folderPath);

                foreach (var filePath in files)
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    content.Add(fileContent, "files", Path.GetFileName(filePath));
                }

                // Call Python API (make sure this URL is for 3D model generation, not the regular YOLO detection)
                var response = await _httpClient.PostAsync("https://hossam203203-yolo-api.hf.space/generate-3d", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);

                    // Extract the model URL returned from Python API
                    if (document.RootElement.TryGetProperty("model_url", out var modelUrlElement))
                    {
                        string modelUrl = modelUrlElement.GetString();

                        // Create a new database connection scope since the original request already completed
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var newScan = new ScanRecord
                        {
                            ArtifactId = artifactId,
                            Date = DateTime.UtcNow,
                            ModelFileUrl = modelUrl
                        };

                        dbContext.ScanRecords.Add(newScan);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error (optional)
                Console.WriteLine($"Error processing 3D job {jobId}: {ex.Message}");
            }
            finally
            {
                // In all cases (success or failure), delete the images folder to avoid filling the hard disk
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
            }
        }
    }
}