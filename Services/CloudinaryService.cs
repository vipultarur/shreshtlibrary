using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly IAdminSettingsService _settingsService;

        public CloudinaryService(IAdminSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folderName = "")
        {
            if (file == null || file.Length == 0) return null;

            var settingsResult = await _settingsService.GetSettingsAsync("super_admin");
            if (!settingsResult.Success) return null;

            var settings = settingsResult.Data as System.Collections.Generic.Dictionary<string, object>;
            if (settings == null) return null;

            var cloudName = settings.ContainsKey("cloudinary_cloud_name") ? settings["cloudinary_cloud_name"]?.ToString() : null;
            var apiKey = settings.ContainsKey("cloudinary_api_key") ? settings["cloudinary_api_key"]?.ToString() : null;
            var apiSecret = settings.ContainsKey("cloudinary_api_secret") ? settings["cloudinary_api_secret"]?.ToString() : null;

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                // Fallback to hardcoded for local dev if empty, or just return null
                cloudName = "diqve4wj";
                apiKey = "615684233812174";
                apiSecret = "IKRgzS7K3OPOP0VAnNN6u-nCw8Q";
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new Cloudinary(account);

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrEmpty(folderName) ? "shresht" : $"shresht/{folderName}"
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.Error != null)
                return null;
                
            var rawUrl = uploadResult.SecureUrl?.ToString();
            if (rawUrl != null && rawUrl.Contains("/upload/"))
            {
                return rawUrl.Replace("/upload/", "/upload/f_auto,q_auto/");
            }
            return rawUrl;
        }
    }
}
