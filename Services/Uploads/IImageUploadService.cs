using Microsoft.AspNetCore.Http;

namespace e_commerce_web_admin.Services.Uploads;

public interface IImageUploadService
{
    Task<ImageUploadResult> UploadAsync(
        IFormFile? file,
        string? folder = null,
        CancellationToken cancellationToken = default);

    Task<ImageDeleteResult> DeleteAsync(
        string publicId,
        CancellationToken cancellationToken = default);
}
