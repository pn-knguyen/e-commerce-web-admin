namespace e_commerce_web_admin.Services.Uploads;

public sealed class ImageUploadResult
{
    public bool Succeeded { get; init; }
    public string? SecureUrl { get; init; }
    public string? PublicId { get; init; }
    public string? OriginalFileName { get; init; }
    public string? ContentType { get; init; }
    public long FileSizeInBytes { get; init; }
    public string? ErrorMessage { get; init; }

    public static ImageUploadResult Success(
        string secureUrl,
        string publicId,
        string? originalFileName,
        string? contentType,
        long fileSizeInBytes)
    {
        return new ImageUploadResult
        {
            Succeeded = true,
            SecureUrl = secureUrl,
            PublicId = publicId,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeInBytes = fileSizeInBytes,
        };
    }

    public static ImageUploadResult Failed(string message)
    {
        return new ImageUploadResult
        {
            Succeeded = false,
            ErrorMessage = message,
        };
    }
}

public sealed class ImageDeleteResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static ImageDeleteResult Success()
    {
        return new ImageDeleteResult { Succeeded = true };
    }

    public static ImageDeleteResult Failed(string message)
    {
        return new ImageDeleteResult
        {
            Succeeded = false,
            ErrorMessage = message,
        };
    }
}
