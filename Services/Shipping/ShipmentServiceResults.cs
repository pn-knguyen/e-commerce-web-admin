namespace e_commerce_web_admin.Services.Shipping;

public sealed class ShipmentActionResult
{
    public bool Found { get; init; } = true;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ShipmentActionResult NotFound(string message = "Không tìm thấy dữ liệu.") =>
        new() { Found = false, Succeeded = false, Message = message };

    public static ShipmentActionResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };

    public static ShipmentActionResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };
}
