using e_commerce_web_admin.ViewModels.Categories;

namespace e_commerce_web_admin.Services.Categories;

public sealed record CategoryValidationError(string FieldName, string Message);

public sealed class CategorySaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public CategoryFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<CategoryValidationError> Errors { get; init; } = Array.Empty<CategoryValidationError>();

    public static CategorySaveResult Success(CategoryFormViewModel form, string message)
    {
        return new CategorySaveResult
        {
            Succeeded = true,
            Form = form,
            Message = message,
        };
    }

    public static CategorySaveResult Failed(CategoryFormViewModel form, IReadOnlyCollection<CategoryValidationError> errors)
    {
        return new CategorySaveResult
        {
            Succeeded = false,
            Form = form,
            Errors = errors,
        };
    }
}

public sealed class CategoryDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static CategoryDeleteResult NotFound()
    {
        return new CategoryDeleteResult { Found = false };
    }

    public static CategoryDeleteResult Success(string message)
    {
        return new CategoryDeleteResult
        {
            Found = true,
            Succeeded = true,
            Message = message,
        };
    }

    public static CategoryDeleteResult Failed(string message)
    {
        return new CategoryDeleteResult
        {
            Found = true,
            Succeeded = false,
            Message = message,
        };
    }
}

public sealed record CategoryToggleResult(bool IsActive);
