using e_commerce_web_admin.ViewModels.Suppliers;

namespace e_commerce_web_admin.Services.Suppliers;

public sealed record SupplierValidationError(string FieldName, string Message);

public sealed class SupplierSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public SupplierFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<SupplierValidationError> Errors { get; init; } = [];

    public static SupplierSaveResult Success(SupplierFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static SupplierSaveResult Failed(
        SupplierFormViewModel form,
        IReadOnlyCollection<SupplierValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class SupplierDeleteCheckResult
{
    public bool Found { get; init; }
    public bool CanDelete { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static SupplierDeleteCheckResult NotFound() => new() { Found = false };

    public static SupplierDeleteCheckResult Allowed(string supplierName) =>
        new()
        {
            Found = true,
            CanDelete = true,
            SupplierName = supplierName,
            Message = $"Có thể xóa nhà cung cấp \"{supplierName}\".",
        };

    public static SupplierDeleteCheckResult Blocked(string supplierName, IReadOnlyList<string> blockers) =>
        new()
        {
            Found = true,
            CanDelete = false,
            SupplierName = supplierName,
            Blockers = blockers,
            Message = $"Không thể xóa \"{supplierName}\" vì còn {string.Join(", ", blockers)} liên quan.",
        };
}

public sealed class SupplierDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static SupplierDeleteResult NotFound() => new() { Found = false };

    public static SupplierDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static SupplierDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record SupplierToggleResult(bool IsActive);
