namespace e_commerce_web_admin.Services.Promotions;

public static class PromotionDateTime
{
    private static readonly TimeZoneInfo AdminTimeZone = ResolveAdminTimeZone();

    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTime ToAdminLocal(DateTime utcDateTime)
    {
        var utcValue = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcValue, AdminTimeZone);
    }

    public static DateTime FromAdminLocal(DateTime localDateTime)
    {
        var localValue = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localValue, AdminTimeZone);
    }

    private static TimeZoneInfo ResolveAdminTimeZone()
    {
        foreach (var timeZoneId in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
