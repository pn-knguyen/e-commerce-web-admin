namespace e_commerce_web_admin.Services.Brands;

public sealed record BrandValidationError(string FieldName, string Message);

public sealed class BrandSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public ViewModels.Brands.BrandFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<BrandValidationError> Errors { get; init; } = Array.Empty<BrandValidationError>();

    public static BrandSaveResult Success(ViewModels.Brands.BrandFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static BrandSaveResult Failed(
        ViewModels.Brands.BrandFormViewModel form,
        IReadOnlyCollection<BrandValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class BrandDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static BrandDeleteResult NotFound() => new() { Found = false };

    public static BrandDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static BrandDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record BrandToggleResult(bool IsActive);
