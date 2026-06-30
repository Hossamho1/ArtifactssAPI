using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Infrastructure.Persistence;
using System.Text.Json;

namespace ArtifactsAPI.Application.Services
{
    public class AIReportService : IAIReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public AIReportService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Creates an AI report for artifact damage analysis by sending the image to the AI service.
        /// Analyzes cracks, calculates damage percentage, and stores the report in the database.
        /// </summary>
        /// <param name="dto">Data transfer object containing ArtifactId, Image, Temperature, and Humidity</param>
        /// <returns>Tuple with success status, error message if failed, and the created AIReport if successful</returns>
        public async Task<(bool IsSuccess, string ErrorMessage, AIReport Data)> CreateReportAsync(CreateAIReportDto dto)
        {
            var artifact = await _context.Artifacts.FindAsync(dto.ArtifactId);
            if (artifact == null)
                return (false, $"Artifact not found. Received ID: {dto.ArtifactId}", null);

            if (dto.Image == null || dto.Image.Length == 0)
                return (false, "Please upload a valid image for analysis.", null);

            bool hasCracks = false;
            float damagePercentage = 0;
            string severity = "Safe";

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = dto.Image.OpenReadStream();
                content.Add(new StreamContent(stream), "file", dto.Image.FileName);

                var response = await _httpClient.PostAsync("https://hossam203203-yolo-api.hf.space/analyze", content);

                if (!response.IsSuccessStatusCode)
                    return (false, "AI service failed to analyze the image.", null);

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                long totalDamageArea = 0;
                long totalStatueArea = 0;

                // Parse damage analysis data from the AI service response
                // Extracts bounding box coordinates to calculate total damage area
                if (root.TryGetProperty("damage_analysis", out var damageProp) && damageProp.ValueKind == JsonValueKind.Array)
                {
                    hasCracks = damageProp.GetArrayLength() > 0;

                    foreach (var item in damageProp.EnumerateArray())
                    {
                        // Read bounding box array containing 4 coordinate values
                        if (item.TryGetProperty("bbox", out var bbox) && bbox.ValueKind == JsonValueKind.Array && bbox.GetArrayLength() >= 4)
                        {
                            float x1 = bbox[0].GetSingle();
                            float y1 = bbox[1].GetSingle();
                            float x2 = bbox[2].GetSingle();
                            float y2 = bbox[3].GetSingle();

                            totalDamageArea += (long)((x2 - x1) * (y2 - y1)); // العرض × الطول
                        }
                    }
                }

                // Parse statue analysis data from the AI service response
                // Calculates the total area of the artifact for damage percentage calculation
                if (root.TryGetProperty("statue_analysis", out var statueProp) && statueProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in statueProp.EnumerateArray())
                    {
                        if (item.TryGetProperty("bbox", out var bbox) && bbox.ValueKind == JsonValueKind.Array && bbox.GetArrayLength() >= 4)
                        {
                            float x1 = bbox[0].GetSingle();
                            float y1 = bbox[1].GetSingle();
                            float x2 = bbox[2].GetSingle();
                            float y2 = bbox[3].GetSingle();

                            totalStatueArea += (long)((x2 - x1) * (y2 - y1));
                        }
                    }
                }

                // Calculate damage percentage based on areas
                // If no full artifact was detected but cracks exist, assign a default damage percentage
                if (totalStatueArea > 0 && totalDamageArea > 0)
                {
                    damagePercentage = ((float)totalDamageArea / totalStatueArea) * 100;
                    if (damagePercentage > 100) damagePercentage = 100;
                }
                else if (hasCracks && totalStatueArea == 0)
                {
                    // If model found cracks but couldn't locate full artifact,
                    // assign default percentage so result is not marked as Safe despite visible damage
                    damagePercentage = 50;
                }

                // Determine severity level based on damage percentage
                severity = damagePercentage > 50 ? "High" : (damagePercentage > 0 ? "Medium" : "Safe");
            }
            catch (Exception ex)
            {
                return (false, $"Could not reach the AI service. Error: {ex.Message}", null);
            }

            // Create and store the AI report with all calculated values
            var newReport = new AIReport
            {
                ArtifactId = artifact.Id,
                HasCracks = hasCracks,
                DamagePercentage = (float)Math.Round(damagePercentage, 2), // Round to 2 decimal places for database storage
                Severity = severity,
                Temperature = decimal.Parse(dto.Temperature),
                Humidity = decimal.Parse(dto.Humidity),
                Date = DateTime.UtcNow
            };

            _context.AIReports.Add(newReport);
            await _context.SaveChangesAsync();

            return (true, null, newReport);
        }
    }
}