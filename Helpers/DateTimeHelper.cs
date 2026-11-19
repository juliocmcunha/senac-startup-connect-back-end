namespace InvestCase.API.Helpers;

/// <summary>
/// The PostgreSQL provider (Npgsql) maps DateTime to "timestamp with time zone",
/// which only accepts DateTime values whose Kind is Utc. Dates typed in Swagger
/// without a "Z" (e.g. "2026-12-10T19:00:00") arrive with Kind=Unspecified and
/// would make SaveChanges throw. These helpers normalize every date to UTC.
/// </summary>
public static class DateTimeHelper
{
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? ToUtc(DateTime? value) => value.HasValue ? ToUtc(value.Value) : null;
}
