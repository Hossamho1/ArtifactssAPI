using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ArtifactsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIReportsController : ControllerBase
    {
        private readonly IAIReportService _reportService;

        private readonly ApplicationDbContext _context; // 👈 Add this

        public AIReportsController(IAIReportService reportService, ApplicationDbContext context)
        {
            _reportService = reportService;
            _context = context; // 👈 Add this
        }
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateReport([FromForm] CreateAIReportDto dto)
        {
            var result = await _reportService.CreateReportAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(new { Message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpGet("debug")]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var count = await _context.Artifacts.CountAsync();
                var first = await _context.Artifacts.FirstOrDefaultAsync();
                return Ok(new { count, first });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}