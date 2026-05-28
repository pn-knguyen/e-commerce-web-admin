namespace e_commerce_web_admin.Services.Specifications;

public sealed record SpecValidationError(string FieldName, string Message);

public sealed class SpecSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public ViewModels.Specifications.SpecificationFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<SpecValidationError> Errors { get; init; } = Array.Empty<SpecValidationError>();

    public static SpecSaveResult Success(ViewModels.Specifications.SpecificationFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static SpecSaveResult Failed(
        ViewModels.Specifications.SpecificationFormViewModel form,
        IReadOnlyCollection<SpecValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class SpecDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static SpecDeleteResult NotFound() => new() { Found = false };
    public static SpecDeleteResult Success(string message) => new() { Found = true, Succeeded = true, Message = message };
    public static SpecDeleteResult Failed(string message) => new() { Found = true, Succeeded = false, Message = message };
}
