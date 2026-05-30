using e_commerce_web_admin.ViewModels.Products;

namespace e_commerce_web_admin.Services.Products;

public sealed record ProductValidationError(string FieldName, string Message);

public sealed class ProductSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public ProductFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<ProductValidationError> Errors { get; init; } = Array.Empty<ProductValidationError>();

    public static ProductSaveResult Success(ProductFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static ProductSaveResult Failed(
        ProductFormViewModel form,
        IReadOnlyCollection<ProductValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class ProductDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static ProductDeleteResult NotFound() => new() { Found = false };

    public static ProductDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static ProductDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed class ProductDeleteCheckResult
{
    public bool Found { get; init; }
    public bool CanDelete { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Blockers { get; init; } = Array.Empty<string>();

    public static ProductDeleteCheckResult NotFound() => new() { Found = false };

    public static ProductDeleteCheckResult Allowed(string productName) =>
        new()
        {
            Found = true,
            CanDelete = true,
            ProductName = productName,
            Message = $"Có thể xoá sản phẩm \"{productName}\".",
        };

    public static ProductDeleteCheckResult Blocked(string productName, IReadOnlyList<string> blockers) =>
        new()
        {
            Found = true,
            CanDelete = false,
            ProductName = productName,
            Blockers = blockers,
            Message = $"Không thể xoá \"{productName}\" vì còn {string.Join(", ", blockers)} liên quan.",
        };
}

public sealed record ProductToggleResult(bool Value);
