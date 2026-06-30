using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;

namespace ArtifactsAPI.Application.Interfaces
{
    public interface IScanRecordService
    {
        Task<(bool IsSuccess, string ErrorMessage, ScanRecord Data)> CreateScanAsync(CreateScanRecordDto dto, string baseUrl);
        Task<(bool IsSuccess, string ErrorMessage, string JobId)> ProcessAndUploadScansAsync(Upload3DScanDto dto);
    }
}
