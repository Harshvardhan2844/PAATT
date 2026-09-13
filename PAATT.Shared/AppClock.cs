namespace PAATT.Shared;

/// <summary>
/// Central place for "what day is it right now" for the whole application.
///
/// Every server and client machine involved (dev laptop, Azure/IIS host, the
/// user's browser) can be in a different time zone, but ASP.NET Core's
/// <see cref="DateTime.UtcNow"/> is always UTC. Comparing a user's chosen
/// "today" against a raw UTC date is wrong for any user who is not in the
/// UTC+0 zone: for roughly 5.5 hours of every day, India Standard Time
/// (UTC+5:30) has already rolled over to a new calendar day while the UTC
/// clock is still on the previous one, so a consultant's genuinely current
/// entry gets rejected as "not today".
///
/// PAATT's users are the company's own staff in India, so we anchor "today"
/// to India Standard Time (a fixed UTC+5:30 offset - India does not observe
/// daylight saving, so a plain offset is safe and avoids depending on the
/// OS time-zone database being installed, which Linux containers sometimes
/// lack).
/// </summary>
public static class AppClock
{
    private static readonly TimeSpan IndiaOffset = TimeSpan.FromHours(5.5);

    /// <summary>The current date in India Standard Time.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow + IndiaOffset);
}
