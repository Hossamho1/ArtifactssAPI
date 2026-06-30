using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool IsSuccess, string Message, object Data)> LoginAsync(LoginDTOs loginDto);
        Task<(bool IsSuccess, string Message)> RegisterAsync(User userDto);
        Task<IEnumerable<User>> GetAllEngineersAsync();

        Task<(bool IsSuccess, string Message)> ChangeEngineerPermissionAsync(int? userId, string email, bool grantPermission);

        Task<(bool IsSuccess, string Message)> DeleteUserByEmailAsync(string email);
        Task<(bool IsSuccess, string Message, string PhotoUrl)> UploadProfilePictureAsync(int userId, IFormFile file, string baseUrl);
        Task<(bool IsSuccess, string Message)> ToggleFollowAsync(int currentUserId, int targetUserId);
        Task<(bool IsSuccess, string Message, object ProfileData)> GetUserProfileAsync(int userId, int currentUserId);
    }
}