using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace e_commerce_web_admin.Services.Uploads;

public sealed class CloudinaryImageUploadService : IImageUploadService
{
    private readonly ILogger<CloudinaryImageUploadService> _logger;
    private readonly CloudinaryOptions _options;

    public CloudinaryImageUploadService(
        IOptions<CloudinaryOptions> options,
        ILogger<CloudinaryImageUploadService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ImageUploadResult> UploadAsync(
        IFormFile? file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            return ImageUploadResult.Failed("Image file is required.");
        }

        var validationError = ValidateUpload(file);
        if (validationError is not null)
        {
            return ImageUploadResult.Failed(validationError);
        }

        if (!HasSignedUploadConfig())
        {
            return ImageUploadResult.Failed(
                "Cloudinary config is missing. Signed upload requires CloudName, ApiKey and ApiSecret.");
        }

        var targetFolder = NormalizeFolder(folder ?? _options.DefaultFolder);
        _logger.LogInformation(
            "Cloudinary upload via SDK. CloudNameConfigured={CloudNameConfigured}, ApiKeyConfigured={ApiKeyConfigured}, ApiSecretConfigured={ApiSecretConfigured}, UploadPresetConfigured={UploadPresetConfigured}, Folder={Folder}",
            HasCloudName(),
            !string.IsNullOrWhiteSpace(GetApiKey()),
            !string.IsNullOrWhiteSpace(GetApiSecret()),
            !string.IsNullOrWhiteSpace(GetUploadPreset()),
            string.IsNullOrWhiteSpace(targetFolder) ? "(none)" : targetFolder);

        await using var stream = file.OpenReadStream();
        var uploadParams = new CloudinaryDotNet.Actions.ImageUploadParams
        {
            File = new FileDescription(Path.GetFileName(file.FileName), stream),
            Folder = targetFolder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false,
        };

        if (!string.IsNullOrWhiteSpace(GetUploadPreset()))
        {
            uploadParams.UploadPreset = GetUploadPreset();
        }

        try
        {
            var uploadResult = await CreateClient().UploadAsync(uploadParams, cancellationToken);
            if (uploadResult.Error is not null)
            {
                return ImageUploadResult.Failed(
                    $"Cloudinary upload failed with status {(int)uploadResult.StatusCode}: {uploadResult.Error.Message}");
            }

            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(uploadResult.PublicId))
            {
                return ImageUploadResult.Failed("Cloudinary response is missing image url or public id.");
            }

            return ImageUploadResult.Success(
                secureUrl,
                uploadResult.PublicId,
                Path.GetFileName(file.FileName),
                file.ContentType,
                uploadResult.Bytes > 0 ? uploadResult.Bytes : file.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Cloudinary upload failed.");
            return ImageUploadResult.Failed($"Cloudinary upload failed: {ex.Message}");
        }
    }

    public async Task<ImageDeleteResult> DeleteAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ImageDeleteResult.Failed("Cloudinary public id is required.");
        }

        if (!HasSignedUploadConfig())
        {
            return ImageDeleteResult.Failed("Cloudinary delete requires CloudName, ApiKey and ApiSecret.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var deleteParams = new CloudinaryDotNet.Actions.DeletionParams(publicId)
        {
            ResourceType = CloudinaryDotNet.Actions.ResourceType.Image,
        };

        try
        {
            var deleteResult = await CreateClient().DestroyAsync(deleteParams);
            cancellationToken.ThrowIfCancellationRequested();

            if (deleteResult.Error is not null)
            {
                return ImageDeleteResult.Failed(
                    $"Cloudinary delete failed with status {(int)deleteResult.StatusCode}: {deleteResult.Error.Message}");
            }

            return string.Equals(deleteResult.Result, "ok", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(deleteResult.Result, "not found", StringComparison.OrdinalIgnoreCase)
                ? ImageDeleteResult.Success()
                : ImageDeleteResult.Failed("Cloudinary did not confirm image deletion.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Cloudinary delete failed for public id {PublicId}.", publicId);
            return ImageDeleteResult.Failed($"Cloudinary delete failed: {ex.Message}");
        }
    }

    private string? ValidateUpload(IFormFile file)
    {
        if (file.Length <= 0)
        {
            return "Image file is empty.";
        }

        if (file.Length > _options.MaxFileSizeInBytes)
        {
            return $"Image file exceeds {_options.MaxFileSizeInBytes} bytes.";
        }

        if (_options.AllowedContentTypes.Count > 0 &&
            !_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return "Image content type is not allowed.";
        }

        return null;
    }

    private Cloudinary CreateClient()
    {
        var account = new Account(GetCloudName(), GetApiKey(), GetApiSecret());
        return new Cloudinary(account);
    }

    private bool HasSignedUploadConfig()
    {
        return HasCloudName() &&
               !string.IsNullOrWhiteSpace(GetApiKey()) &&
               !string.IsNullOrWhiteSpace(GetApiSecret());
    }

    private bool HasCloudName()
    {
        return !string.IsNullOrWhiteSpace(GetCloudName());
    }

    private string GetCloudName()
    {
        return _options.CloudName.Trim();
    }

    private string GetApiKey()
    {
        return _options.ApiKey.Trim();
    }

    private string GetApiSecret()
    {
        return _options.ApiSecret.Trim();
    }

    private string GetUploadPreset()
    {
        return _options.UploadPreset.Trim();
    }

    private static string NormalizeFolder(string? folder)
    {
        return string.IsNullOrWhiteSpace(folder)
            ? string.Empty
            : folder.Trim().Trim('/');
    }
}
