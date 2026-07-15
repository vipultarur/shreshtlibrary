using System.Threading.Tasks;
using System.Linq;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Student;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System;
using System.Threading;
using WebApplication1.Repositories;

namespace WebApplication1.Services
{
    public class StudentProfileService : IStudentProfileService
    {
        private readonly IStudentRepository _repository;
        private readonly IWebHostEnvironment _env;
        private readonly ICloudinaryService _cloudinary;

        public StudentProfileService(IStudentRepository repository, IWebHostEnvironment env, ICloudinaryService cloudinary)
        {
            _repository = repository;
            _env = env;
            _cloudinary = cloudinary;
        }

        public async Task<ApiResponse<object>?> GetProfileAsync(long userId, string scheme, string host, CancellationToken ct = default)
        {
            var profile = await _repository.GetProfileWithUserAsync(userId, ct);

            if (profile == null) return null;

            return ApiResponse<object>.Ok(new
            {
                username = profile.User.Username,
                first_name = profile.User.FirstName,
                last_name = profile.User.LastName,
                email = profile.User.Email,
                mobile = profile.User.Mobile,
                goal = profile.Goal,
                dob = profile.Dob?.ToString("yyyy-MM-dd"),
                caste = profile.Caste,
                address = profile.Address,
                profile_photo = !string.IsNullOrEmpty(profile.ProfilePhoto) ? (profile.ProfilePhoto.StartsWith("http") ? profile.ProfilePhoto : $"{scheme}://{host}/media/{profile.ProfilePhoto}") : (string?)null,
                parent_mobile = profile.ParentMobile
            });
        }

        public async Task<ApiResponse<object>?> UpdateProfileAsync(long userId, UpdateProfileDto dto, string scheme, string host, CancellationToken ct = default)
        {
            var user = await _repository.GetUserWithProfileAsync(userId, ct);

            if (user == null || user.StudentsStudentprofile == null)
                return null;

            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Goal))
                user.StudentsStudentprofile.Goal = dto.Goal;
            if (!string.IsNullOrEmpty(dto.Dob) && System.DateOnly.TryParse(dto.Dob, out var dob))
                user.StudentsStudentprofile.Dob = dob;
            if (dto.Caste != null)
                user.StudentsStudentprofile.Caste = dto.Caste;
            if (dto.Address != null)
                user.StudentsStudentprofile.Address = dto.Address;
            if (dto.ParentMobile != null)
                user.StudentsStudentprofile.ParentMobile = dto.ParentMobile;

            user.StudentsStudentprofile.UpdatedAt = System.DateTime.UtcNow;
            await _repository.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new
            {
                username = user.Username,
                first_name = user.FirstName,
                last_name = user.LastName,
                email = user.Email,
                mobile = user.Mobile,
                goal = user.StudentsStudentprofile.Goal,
                dob = user.StudentsStudentprofile.Dob?.ToString("yyyy-MM-dd"),
                caste = user.StudentsStudentprofile.Caste,
                address = user.StudentsStudentprofile.Address,
                profile_photo = !string.IsNullOrEmpty(user.StudentsStudentprofile.ProfilePhoto) ? (user.StudentsStudentprofile.ProfilePhoto.StartsWith("http") ? user.StudentsStudentprofile.ProfilePhoto : $"{scheme}://{host}/media/{user.StudentsStudentprofile.ProfilePhoto}") : (string?)null,
                parent_mobile = user.StudentsStudentprofile.ParentMobile
            });
        }

        public async Task<ApiResponse<object>?> GetIdCardAsync(long userId, string scheme, string host, CancellationToken ct = default)
        {
            var profile = await _repository.GetProfileWithUserAsync(userId, ct);

            if (profile == null) return null;

            return ApiResponse<object>.Ok(new
            {
                student_id = profile.UserId,
                full_name = $"{profile.User.FirstName} {profile.User.LastName}".Trim(),
                mobile = profile.User.Mobile,
                email = profile.User.Email,
                goal = profile.Goal,
                dob = profile.Dob?.ToString("yyyy-MM-dd"),
                photo_url = !string.IsNullOrEmpty(profile.ProfilePhoto) ? (profile.ProfilePhoto.StartsWith("http") ? profile.ProfilePhoto : $"{scheme}://{host}/media/{profile.ProfilePhoto}") : (string?)null,
                qr_data = $"SHR-{profile.UserId}"
            });
        }

        public async Task<ApiResponse<object>> UploadPhotoAsync(long userId, IFormFile profile_photo, string scheme, string host, CancellationToken ct = default)
        {
            if (profile_photo == null || profile_photo.Length == 0)
                return ApiResponse<object>.Fail("No file uploaded");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(profile_photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) 
                return ApiResponse<object>.Fail("Invalid file type.");

            if (profile_photo.Length > 5 * 1024 * 1024) 
                return ApiResponse<object>.Fail("File size exceeds 5MB limit.");

            var profile = await _repository.GetProfileWithUserAsync(userId, ct);
            if (profile == null) return ApiResponse<object>.Fail("Profile not found");

            var cloudinaryUrl = await _cloudinary.UploadImageAsync(profile_photo, "profiles");
            if (!string.IsNullOrEmpty(cloudinaryUrl))
            {
                profile.ProfilePhoto = cloudinaryUrl;
            }
            else
            {
                return ApiResponse<object>.Fail("Failed to upload profile photo to Cloudinary.");
            }

            await _repository.SaveChangesAsync(ct);

            var photoUrl = profile.ProfilePhoto.StartsWith("http") ? profile.ProfilePhoto : $"{scheme}://{host}/media/{profile.ProfilePhoto}";
            return ApiResponse<object>.Ok(new { photo_url = photoUrl });
        }
    }
}
