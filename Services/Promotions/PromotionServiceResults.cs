namespace e_commerce_web_admin.Services.Promotions;

public sealed record PromotionValidationError(string FieldName, string Message);

public sealed class PromotionSaveResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public PromotionFormData Form { get; init; } = new();
    public IReadOnlyCollection<PromotionValidationError> Errors { get; init; } = [];

    public static PromotionSaveResult Success(PromotionFormData form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static PromotionSaveResult Failed(
        PromotionFormData form,
        IReadOnlyCollection<PromotionValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class PromotionDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static PromotionDeleteResult NotFound() => new() { Found = false };

    public static PromotionDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static PromotionDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record PromotionToggleResult(bool IsActive);
