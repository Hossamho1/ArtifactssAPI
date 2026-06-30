using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Interfaces
{
    public interface IArtifactService 
    {
        Task<IEnumerable<Artifact>> GetAllArtifactsAsync();
        Task<Artifact> GetArtifactByIdAsync(int id);
        Task<Artifact> CreateArtifactAsync(Artifact artifact);

        Task<(bool IsSuccess, bool IsNotFound)> UpdateArtifactAsync(int id, Artifact artifact);

        Task<bool> DeleteArtifactAsync(int id);
    }
}