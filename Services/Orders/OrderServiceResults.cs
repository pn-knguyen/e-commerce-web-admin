namespace e_commerce_web_admin.Services.Orders;

public sealed record OrderValidationError(string FieldName, string Message);

public sealed class OrderStatusUpdateResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<OrderValidationError> Errors { get; init; } = [];

    public static OrderStatusUpdateResult NotFound() => new() { Found = false };

    public static OrderStatusUpdateResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static OrderStatusUpdateResult Failed(IReadOnlyCollection<OrderValidationError> errors) =>
        new() { Found = true, Succeeded = false, Errors = errors };
}
