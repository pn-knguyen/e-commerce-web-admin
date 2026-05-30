namespace e_commerce_web_admin.Services.Vouchers;

public static class VoucherDateTime
{
    private const string WindowsTimeZoneId = "SE Asia Standard Time";
    private const string IanaTimeZoneId = "Asia/Ho_Chi_Minh";

    public static TimeZoneInfo AdminTimeZone { get; } = ResolveAdminTimeZone();

    public static DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    public static DateTime ToAdminLocal(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, AdminTimeZone);
    }

    public static DateTime FromAdminLocal(DateTime localDateTime)
    {
        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, AdminTimeZone);
    }

    private static TimeZoneInfo ResolveAdminTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZoneId);
        }
    }
}
