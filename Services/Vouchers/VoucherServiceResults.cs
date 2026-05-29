using e_commerce_web_admin.ViewModels.Vouchers;

namespace e_commerce_web_admin.Services.Vouchers;

public sealed record VoucherValidationError(string FieldName, string Message);

public sealed class VoucherSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public VoucherFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<VoucherValidationError> Errors { get; init; } = Array.Empty<VoucherValidationError>();

    public static VoucherSaveResult Success(VoucherFormViewModel form, string message)
    {
        return new VoucherSaveResult
        {
            Succeeded = true,
            Form = form,
            Message = message,
        };
    }

    public static VoucherSaveResult Failed(
        VoucherFormViewModel form,
        IReadOnlyCollection<VoucherValidationError> errors)
    {
        return new VoucherSaveResult
        {
            Succeeded = false,
            Form = form,
            Errors = errors,
        };
    }
}

public sealed class VoucherDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public static VoucherDeleteResult NotFound()
    {
        return new VoucherDeleteResult { Found = false };
    }

    public static VoucherDeleteResult Success(string message)
    {
        return new VoucherDeleteResult
        {
            Found = true,
            Succeeded = true,
            Message = message,
        };
    }

    public static VoucherDeleteResult Failed(string message)
    {
        return new VoucherDeleteResult
        {
            Found = true,
            Succeeded = false,
            Message = message,
        };
    }
}

public sealed record VoucherToggleResult(bool IsActive);
