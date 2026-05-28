namespace e_commerce_web_admin.Services.Uploads;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string UploadPreset { get; set; } = string.Empty;
    public string DefaultFolder { get; set; } = "ecommerce-admin";
    public long MaxFileSizeInBytes { get; set; } = 5 * 1024 * 1024;
    public List<string> AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    ];
}
