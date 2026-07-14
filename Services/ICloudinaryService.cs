using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folderName = "");
    }
}
