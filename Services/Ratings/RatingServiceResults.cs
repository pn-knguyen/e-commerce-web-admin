namespace e_commerce_web_admin.Services.Ratings;

public sealed record RatingToggleResult(bool IsApproved, string Message);

public sealed class RatingDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static RatingDeleteResult NotFound() => new() { Found = false };

    public static RatingDeleteResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static RatingDeleteResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}
