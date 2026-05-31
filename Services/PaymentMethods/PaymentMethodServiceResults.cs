using e_commerce_web_admin.ViewModels.PaymentMethods;

namespace e_commerce_web_admin.Services.PaymentMethods;

public sealed record PaymentMethodValidationError(string FieldName, string Message);

public sealed class PaymentMethodSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public PaymentMethodFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<PaymentMethodValidationError> Errors { get; init; } = [];

    public static PaymentMethodSaveResult Success(PaymentMethodFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static PaymentMethodSaveResult Failed(
        PaymentMethodFormViewModel form,
        IReadOnlyCollection<PaymentMethodValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class PaymentMethodDeleteCheckResult
{
    public bool Found { get; init; }
    public bool CanDelete { get; init; }
    public string MethodName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static PaymentMethodDeleteCheckResult NotFound() => new() { Found = false };

    public static PaymentMethodDeleteCheckResult Allowed(string methodName) =>
        new()
        {
            Found = true,
            CanDelete = true,
            MethodName = methodName,
            Message = $"Có thể xóa phương thức thanh toán \"{methodName}\".",
        };

    public static PaymentMethodDeleteCheckResult Blocked(string methodName, IReadOnlyList<string> blockers) =>
        new()
        {
            Found = true,
            CanDelete = false,
            MethodName = methodName,
            Blockers = blockers,
            Message = $"Không thể xóa \"{methodName}\" vì còn {string.Join(", ", blockers)} liên quan.",
        };
}

public sealed class PaymentMethodDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static PaymentMethodDeleteResult NotFound() => new() { Found = false };

    public static PaymentMethodDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static PaymentMethodDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record PaymentMethodToggleResult(bool Value);
