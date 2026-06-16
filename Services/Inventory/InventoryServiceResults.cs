using e_commerce_web_admin.ViewModels.Inventory;

namespace e_commerce_web_admin.Services.Inventory;

public sealed record InventoryValidationError(string FieldName, string Message);

public sealed class GoodsReceiptSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public GoodsReceiptFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<InventoryValidationError> Errors { get; init; } = [];

    public static GoodsReceiptSaveResult Success(GoodsReceiptFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static GoodsReceiptSaveResult Failed(
        GoodsReceiptFormViewModel form,
        IReadOnlyCollection<InventoryValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class GoodsReceiptActionResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static GoodsReceiptActionResult NotFound() => new() { Found = false };

    public static GoodsReceiptActionResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static GoodsReceiptActionResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}
