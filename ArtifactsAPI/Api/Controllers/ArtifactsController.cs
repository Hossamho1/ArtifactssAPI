using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtifactsAPI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Tourist & Engineer can view
    public class ArtifactsController : ControllerBase
    {
        private readonly IArtifactService _artifactService;

        public ArtifactsController(IArtifactService artifactService)
        {
            _artifactService = artifactService;
        }

        // GET: api/Artifacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Artifact>>> GetArtifacts()
        {
            var artifacts = await _artifactService.GetAllArtifactsAsync();
            return Ok(artifacts);
        }

        // GET: api/Artifacts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Artifact>> GetArtifact(int id)
        {
            var artifact = await _artifactService.GetArtifactByIdAsync(id);

            if (artifact == null)
            {
                return NotFound();
            }

            return Ok(artifact);
        }

        // PUT: api/Artifacts/5
        [Authorize(Roles = "Engineer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutArtifact(int id, Artifact artifact)
        {
            var result = await _artifactService.UpdateArtifactAsync(id, artifact);

            if (!result.IsSuccess)
            {
                if (result.IsNotFound) return NotFound();
                return BadRequest();
            }

            return NoContent();
        }

        // POST: api/Artifacts
        [Authorize(Roles = "Engineer")]
        [HttpPost]
        public async Task<ActionResult<Artifact>> PostArtifact(Artifact artifact)
        {
            var createdArtifact = await _artifactService.CreateArtifactAsync(artifact);

            return CreatedAtAction(nameof(GetArtifact), new { id = createdArtifact.Id }, createdArtifact);
        }

        // DELETE: api/Artifacts/5
        [Authorize(Roles = "Engineer")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArtifact(int id)
        {
            var success = await _artifactService.DeleteArtifactAsync(id);

            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}