using e_commerce_web_admin.ViewModels.ProductVariants;

namespace e_commerce_web_admin.Services.ProductVariants;

public sealed record ProductVariantValidationError(string FieldName, string Message);

public sealed class ProductVariantSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public ProductVariantFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<ProductVariantValidationError> Errors { get; init; } = [];

    public static ProductVariantSaveResult Success(ProductVariantFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static ProductVariantSaveResult Failed(
        ProductVariantFormViewModel form,
        IReadOnlyCollection<ProductVariantValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class ProductVariantDeleteCheckResult
{
    public bool Found { get; init; }
    public bool CanDelete { get; init; }
    public string VariantCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static ProductVariantDeleteCheckResult NotFound() => new() { Found = false };

    public static ProductVariantDeleteCheckResult Allowed(string variantCode) =>
        new()
        {
            Found = true,
            CanDelete = true,
            VariantCode = variantCode,
            Message = $"Có thể xóa biến thể \"{variantCode}\".",
        };

    public static ProductVariantDeleteCheckResult Blocked(
        string variantCode,
        IReadOnlyList<string> blockers) =>
        new()
        {
            Found = true,
            CanDelete = false,
            VariantCode = variantCode,
            Blockers = blockers,
            Message = $"Không thể xóa \"{variantCode}\" vì còn {string.Join(", ", blockers)} liên quan.",
        };
}

public sealed class ProductVariantDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static ProductVariantDeleteResult NotFound() => new() { Found = false };

    public static ProductVariantDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static ProductVariantDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record ProductVariantToggleResult(bool Value);
