using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Domain.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Interfaces;

public interface IPostService
{
    Task<IEnumerable<Post>> GetAllPostsAsync();
    Task<(bool IsSuccess, string ErrorMessage, object Data)> CreatePostAsync(CreatePostDto request, int userId, string baseUrl);
    Task<(bool IsSuccess, string ErrorMessage)> EditPostAsync(int id, UpdatePostDto request, int currentUserId);
    Task<(bool IsSuccess, string ErrorMessage)> DeletePostAsync(int id, int currentUserId, string currentUserRole);
    Task<(bool IsSuccess, string Message)> ToggleLikeAsync(int postId, int userId);
    Task<(bool IsSuccess, string Message)> ToggleBookmarkAsync(int postId, int userId);
    Task<(bool IsSuccess, string Message, int ViewsCount)> RecordPostViewAsync(int postId, int userId);
    Task<object> GetPostByIdAsync(int postId, int currentUserId);
}