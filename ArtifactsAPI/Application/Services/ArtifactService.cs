using ArtifactsAPI.Application.Interfaces;

using ArtifactsAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace ArtifactsAPI.Application.Services
{
    public class ArtifactService : IArtifactService
    {
        private readonly ApplicationDbContext _context;

        public ArtifactService(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Retrieves all artifacts from the database.
        /// </summary>
        /// <returns>Collection of all artifacts</returns>
        public async Task<IEnumerable<Artifact>> GetAllArtifactsAsync()
        {
            return await _context.Artifacts.ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific artifact by its ID.
        /// </summary>
        /// <param name="id">The artifact ID to search for</param>
        /// <returns>The artifact if found; null otherwise</returns>
        public async Task<Artifact?> GetArtifactByIdAsync(int id)
        {
            return await _context.Artifacts.FindAsync(id);
        }

        /// <summary>
        /// Creates and stores a new artifact in the database.
        /// </summary>
        /// <param name="artifact">The artifact entity to create</param>
        /// <returns>The created artifact with its generated ID</returns>
        public async Task<Artifact> CreateArtifactAsync(Artifact artifact)
        {
            _context.Artifacts.Add(artifact);
            await _context.SaveChangesAsync();
            return artifact;
        }

        /// <summary>
        /// Updates an existing artifact in the database.
        /// </summary>
        /// <param name="id">The artifact ID to update</param>
        /// <param name="artifact">The updated artifact data</param>
        /// <returns>Tuple with success status and whether artifact was not found</returns>
        public async Task<(bool IsSuccess, bool IsNotFound)> UpdateArtifactAsync(int id, Artifact artifact)
        {
            if (id != artifact.Id)
            {
                return (false, false); // BadRequest (ID mismatch)
            }

            _context.Artifacts.Update(artifact);

            try
            {
                await _context.SaveChangesAsync();
                return (true, false); // Success
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArtifactExists(id))
                {
                    return (false, true); // NotFound
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Deletes an artifact from the database by its ID.
        /// </summary>
        /// <param name="id">The artifact ID to delete</param>
        /// <returns>True if deletion was successful; false if artifact not found</returns>
        public async Task<bool> DeleteArtifactAsync(int id)
        {
            var artifact = await _context.Artifacts.FindAsync(id);
            if (artifact == null)
            {
                return false; 
            }

            _context.Artifacts.Remove(artifact);
            await _context.SaveChangesAsync();
            return true; 
        }

        /// <summary>
        /// Checks if an artifact exists in the database.
        /// </summary>
        /// <param name="id">The artifact ID to check</param>
        /// <returns>True if artifact exists; false otherwise</returns>
        private bool ArtifactExists(int id)
        {
            return _context.Artifacts.FindAsync(id) != null;
        }
    }
}