namespace e_commerce_web_admin.Services.Attributes;

// ── Attribute save ─────────────────────────────────────────────────────────

public sealed record AttrValidationError(string Field, string Message);

public sealed class AttrSaveResult
{
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public IReadOnlyList<AttrValidationError> Errors { get; private init; } = [];

    public static AttrSaveResult Success(string message) => new() { Succeeded = true, Message = message };

    public static AttrSaveResult Failed(IEnumerable<AttrValidationError> errors) => new()
    {
        Succeeded = false,
        Message = "Dữ liệu không hợp lệ.",
        Errors = errors.ToList()
    };
}

// ── Attribute delete ───────────────────────────────────────────────────────

public sealed class AttrDeleteResult
{
    public bool Found { get; private init; }
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static AttrDeleteResult NotFound() => new() { Found = false, Message = "Không tìm thấy thuộc tính." };
    public static AttrDeleteResult Failed(string message) => new() { Found = true, Succeeded = false, Message = message };
    public static AttrDeleteResult Success(string message) => new() { Found = true, Succeeded = true, Message = message };
}

// ── Option save ────────────────────────────────────────────────────────────

public sealed class AttrOptionSaveResult
{
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public long OptionId { get; private init; }

    public static AttrOptionSaveResult Success(long optionId, string message) =>
        new() { Succeeded = true, OptionId = optionId, Message = message };

    public static AttrOptionSaveResult Failed(string message) =>
        new() { Succeeded = false, Message = message };
}

// ── Option delete ──────────────────────────────────────────────────────────

public sealed class AttrOptionDeleteResult
{
    public bool Found { get; private init; }
    public bool Succeeded { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static AttrOptionDeleteResult NotFound() => new() { Found = false, Message = "Không tìm thấy option." };
    public static AttrOptionDeleteResult Failed(string message) => new() { Found = true, Succeeded = false, Message = message };
    public static AttrOptionDeleteResult Success(string message) => new() { Found = true, Succeeded = true, Message = message };
}
