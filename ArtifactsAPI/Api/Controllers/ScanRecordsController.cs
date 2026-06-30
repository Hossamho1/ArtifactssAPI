using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ArtifactsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScanRecordsController : ControllerBase
    {
        private readonly IScanRecordService _scanService;

        public ScanRecordsController(IScanRecordService scanService)
        {
            _scanService = scanService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateScan([FromForm] CreateScanRecordDto dto)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = await _scanService.CreateScanAsync(dto, baseUrl);

            if (!result.IsSuccess)
            {
                return BadRequest(new { Message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }


        [HttpPost("upload-scans")]
        public async Task<IActionResult> UploadScans([FromForm] Upload3DScanDto dto)
        {
            var result = await _scanService.ProcessAndUploadScansAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Accepted(new
            {
                Message = "Images received successfully. 3D Model generation started in the background.",
                JobId = result.JobId
            });
        }


    }
}