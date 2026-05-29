namespace e_commerce_web_admin.Services.CategoryVariantAttributes;

public sealed record CvaSaveResult(bool Succeeded, string Message);

public sealed record CvaRemoveResult(bool Found, bool Succeeded, string Message)
{
    public static CvaRemoveResult NotFound() => new(false, false, "Không tìm thấy liên kết.");
    public static CvaRemoveResult Blocked(string msg) => new(true, false, msg);
    public static CvaRemoveResult Ok(string msg) => new(true, true, msg);
}
