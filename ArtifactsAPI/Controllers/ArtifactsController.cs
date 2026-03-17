using ArtifactsAPI.Data;
using ArtifactsAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtifactsAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // 👈 السطر ده هو اللي بيخلي تيم الفلاتر لازم يبعت الـ Token في الـ Header
public class ArtifactsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ArtifactsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Artifacts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetArtifacts()
    {
        return await _context.Artifacts.ToListAsync();
    }

    // GET: api/Artifacts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Artifact>> GetArtifact(int id)
    {
        var artifact = await _context.Artifacts.FindAsync(id);

        if (artifact == null)
        {
            return NotFound();
        }

        return artifact;
    }

    // PUT: api/Artifacts/5
    [Authorize(Roles = "Engineer")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutArtifact(int id, Artifact artifact)
    {
        if (id != artifact.Id)
        {
            return BadRequest();
        }

        _context.Entry(artifact).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ArtifactExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Artifacts
    [Authorize(Roles = "Engineer")]
    [HttpPost]
    public async Task<ActionResult<Artifact>> PostArtifact(Artifact artifact)
    {
        _context.Artifacts.Add(artifact);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetArtifact", new { id = artifact.Id }, artifact);
    }

    // DELETE: api/Artifacts/5
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Engineer")]
    public async Task<IActionResult> DeleteArtifact(int id)
    {
        var artifact = await _context.Artifacts.FindAsync(id);
        if (artifact == null)
        {
            return NotFound();
        }

        _context.Artifacts.Remove(artifact);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ArtifactExists(int id)
    {
        return _context.Artifacts.Any(e => e.Id == id);
    }
}
