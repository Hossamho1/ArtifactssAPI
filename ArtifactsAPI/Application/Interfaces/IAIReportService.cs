using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Interfaces
{
    public interface IAIReportService
    {
        Task<(bool IsSuccess, string ErrorMessage, AIReport Data)> CreateReportAsync(CreateAIReportDto dto);
    }
}