using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Domain.Models;
using ArtifactsAPI.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IValidator<CreatePostDto> _validator;

        public PostService(ApplicationDbContext context, IWebHostEnvironment env, IValidator<CreatePostDto> validator)
        {
            _context = context;
            _env = env;
            _validator = validator;
        }

        /// <summary>
        /// Retrieves all posts from the database with their coordinates and user information.
        /// Posts are ordered by ID in descending order (newest first).
        /// </summary>
        /// <returns>Collection of all posts ordered by creation date</returns>
        public async Task<IEnumerable<Post>> GetAllPostsAsync()
        {
            return await _context.Posts
                .Include(p => p.Coordinates)
                .Include(p => p.User)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new post with file uploads for cover photo and 3D model.
        /// Automatically creates an associated Artifact for the post.
        /// Deserializes JSON coordinates string into Coordinate objects.
        /// </summary>
        /// <param name="request">Post creation DTO with files and coordinate JSON</param>
        /// <param name="userId">The ID of the user creating the post</param>
        /// <param name="baseUrl">The base URL for constructing file access URLs</param>
        /// <returns>Tuple with success status, error message if failed, and post data if successful</returns>
        public async Task<(bool IsSuccess, string ErrorMessage, object Data)> CreatePostAsync(CreatePostDto request, int userId, string baseUrl)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return (false, errors, null);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.", null);
            if (!user.CanCreatePosts) return (false, "Access Denied. An Admin must grant you permission to create posts.", null);

            string finalPhotoUrl = "";
            string finalModelUrl = "";
            string uploadsFolder = !string.IsNullOrEmpty(_env.WebRootPath)
                ? Path.Combine(_env.WebRootPath, "uploads")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            if (request.CoverPhoto != null)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.CoverPhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.CoverPhoto.CopyToAsync(fileStream);
                }
                finalPhotoUrl = $"{baseUrl}/uploads/{uniqueFileName}";
            }

            if (request.Model3D != null)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.Model3D.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Model3D.CopyToAsync(fileStream);
                }
                finalModelUrl = $"{baseUrl}/uploads/{uniqueFileName}";
            }

            // Create Artifact first
            var artifact = new Artifact
            {
                Name = request.Title,
                History = request.Description,
                Location = "New Discovery"
            };
            _context.Artifacts.Add(artifact);
            await _context.SaveChangesAsync();

            var newPost = new Post
            {
                Title = request.Title,
                Description = request.Description,
                CoverPhoto = finalPhotoUrl,
                Model3D = finalModelUrl,
                UserId = userId,
                ArtifactId = artifact.Id,
                Artifact = artifact
            };

            if (!string.IsNullOrEmpty(request.CoordinatesJson))
            {
                try
                {
                    newPost.Coordinates = JsonSerializer.Deserialize<List<Coordinate>>(request.CoordinatesJson) ?? new List<Coordinate>();
                }
                catch (JsonException)
                {
                    return (false, "Invalid JSON format for coordinates.", null);
                }
            }

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            var resultData = new { Message = "Post created successfully!", PostId = newPost.Id, CoverPhotoUrl = finalPhotoUrl, Model3DUrl = finalModelUrl };
            return (true, null, resultData);
        }

        /// <summary>
        /// Updates an existing post's title and description.
        /// Only the post owner can edit their own posts (strict ownership check).
        /// </summary>
        /// <param name="id">The ID of the post to edit</param>
        /// <param name="request">The updated post data</param>
        /// <param name="currentUserId">The ID of the user attempting to edit</param>
        /// <returns>Tuple with success status and error message if failed</returns>
        public async Task<(bool IsSuccess, string ErrorMessage)> EditPostAsync(int id, UpdatePostDto request, int currentUserId)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return (false, "Post not found.");

            // STRICT OWNERSHIP CHECK
            if (post.UserId != currentUserId) return (false, "Access Denied. You can only edit your own posts.");

            post.Title = request.Title ?? post.Title;
            post.Description = request.Description ?? post.Description;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>
        /// Deletes a post from the database.
        /// Only the post owner or an Admin can delete the post.
        /// </summary>
        /// <param name="id">The ID of the post to delete</param>
        /// <param name="currentUserId">The ID of the user attempting to delete</param>
        /// <param name="currentUserRole">The role of the user attempting to delete (Admin, Engineer, etc.)</param>
        /// <returns>Tuple with success status and error message if failed</returns>
        public async Task<(bool IsSuccess, string ErrorMessage)> DeletePostAsync(int id, int currentUserId, string currentUserRole)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return (false, "Post not found.");

            bool isOwner = (post.UserId == currentUserId);
            bool isAdmin = (currentUserRole == "Admin");

            if (!isOwner && !isAdmin) return (false, "Access Denied. You must be the owner of the post or an Admin to delete it.");

            // Optionally: Add logic here to delete the physical files from wwwroot before removing the post

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ... (ToggleLikeAsync, ToggleBookmarkAsync, RecordPostViewAsync implemented similarly returning (bool, string) )

        /// <summary>
        /// Retrieves a single post by ID with all its metadata.
        /// Includes author information, like count, view count, and current user's like/bookmark status.
        /// </summary>
        /// <param name="postId">The ID of the post to retrieve</param>
        /// <param name="currentUserId">The ID of the current user (for determining interaction status)</param>
        /// <returns>Post object with metadata if found; null otherwise</returns>
        public async Task<object> GetPostByIdAsync(int postId, int currentUserId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return null;

            var author = await _context.Users.FindAsync(post.UserId);
            int totalLikes = await _context.PostLikes.CountAsync(l => l.PostId == postId);
            int totalViews = await _context.PostViews.CountAsync(pv => pv.PostId == postId);
            bool isLikedByMe = await _context.PostLikes.AnyAsync(l => l.PostId == postId && l.UserId == currentUserId);
            bool isBookmarkedByMe = await _context.Bookmarks.AnyAsync(b => b.PostId == postId && b.UserId == currentUserId);

            return new
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CoverPhotoUrl = post.CoverPhoto,
                Model3DUrl = post.Model3D,
                Coordinates = post.Coordinates,
                Author = new { Id = author?.Id, Name = author?.Name ?? "Unknown Engineer", ProfilePictureUrl = author?.ProfilePicture },
                LikesCount = totalLikes,
                ViewsCount = totalViews,
                IsLiked = isLikedByMe,
                IsBookmarked = isBookmarkedByMe
            };
        }

        /// <summary>
        /// Toggles the like status of a post by a user.
        /// If already liked, removes the like; otherwise, creates a new like.
        /// </summary>
        /// <param name="postId">The ID of the post to like/unlike</param>
        /// <param name="userId">The ID of the user liking/unliking the post</param>
        /// <returns>Tuple with success status and action message</returns>
        public async Task<(bool IsSuccess, string Message)> ToggleLikeAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return (false, "Post not found.");

            var existingLike = await _context.PostLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return (true, "Post Unliked");
            }

            _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
            await _context.SaveChangesAsync();
            return (true, "Post Liked");
        }

        /// <summary>
        /// Toggles the bookmark status of a post by a user.
        /// If already bookmarked, removes the bookmark; otherwise, creates a new bookmark.
        /// </summary>
        /// <param name="postId">The ID of the post to bookmark/unbookmark</param>
        /// <param name="userId">The ID of the user bookmarking/unbookmarking the post</param>
        /// <returns>Tuple with success status and action message</returns>
        public async Task<(bool IsSuccess, string Message)> ToggleBookmarkAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return (false, "Post not found.");

            var existingBookmark = await _context.Bookmarks.FirstOrDefaultAsync(b => b.PostId == postId && b.UserId == userId);

            if (existingBookmark != null)
            {
                _context.Bookmarks.Remove(existingBookmark);
                await _context.SaveChangesAsync();
                return (true, "Bookmark removed");
            }

            _context.Bookmarks.Add(new Bookmark { PostId = postId, UserId = userId });
            await _context.SaveChangesAsync();
            return (true, "Post Bookmarked");
        }

        /// <summary>
        /// Records a view of a post by a user.
        /// Each user can only count once per post (prevents double-counting views).
        /// Returns the total view count after recording.
        /// </summary>
        /// <param name="postId">The ID of the post being viewed</param>
        /// <param name="userId">The ID of the user viewing the post</param>
        /// <returns>Tuple with success status, message, and total views count</returns>
        public async Task<(bool IsSuccess, string Message, int ViewsCount)> RecordPostViewAsync(int postId, int userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return (false, "Post not found.", 0);

            var existingView = await _context.PostViews
                .FirstOrDefaultAsync(pv => pv.PostId == postId && pv.UserId == userId);

            if (existingView == null)
            {
                _context.PostViews.Add(new PostView { PostId = postId, UserId = userId });
                await _context.SaveChangesAsync();
            }

            int totalViews = await _context.PostViews.CountAsync(pv => pv.PostId == postId);

            return (true, "View recorded successfully.", totalViews);
        }
    }
}