namespace App.Shared;

/// <summary>
/// The calendar and limit rules that both the service layer and the UI have
/// to agree on. They live here so there's one definition of "which day a week
/// starts on" and "how many hours a day is too many" instead of a copy in
/// each layer that can drift.
///
/// Note on "today": every date decision in this app uses the server's local
/// date (DateTime.Today). With consultants in a single timezone that's fine.
/// Spread them across timezones and someone's "today" will close before or
/// after they expect it — worth revisiting in Phase 6 if that's a real case.
/// </summary>
public static class TimeRules
{
    /// <summary>
    /// A timesheet week runs Monday to Sunday. Changing this constant changes
    /// it everywhere; existing Timesheet.WeekStartingDate rows would need
    /// migrating, so decide before there's real data.
    /// </summary>
    public const DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;

    /// <summary>
    /// Hard cap on hours a consultant can log for one date, summed across
    /// every project they work on — not per timesheet. Enforced in
    /// TimesheetEntryService; the UI uses it to show how much room is left.
    /// </summary>
    public const decimal MaxHoursPerDay = 24m;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Monday of the week containing <paramref name="date"/>.</summary>
    public static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
        return date.AddDays(-offset);
    }

    public static DateOnly CurrentWeekStart() => StartOfWeek(Today);

    /// <summary>The seven dates of the week starting at <paramref name="weekStart"/>.</summary>
    public static IReadOnlyList<DateOnly> DaysOfWeek(DateOnly weekStart) =>
        Enumerable.Range(0, 7).Select(weekStart.AddDays).ToList();

    /// <summary>Round-trip format used in URLs and query strings.</summary>
    public static string ToRouteValue(DateOnly date) => date.ToString("yyyy-MM-dd");

    /// <summary>
    /// Parses a route/query value back to a week start, snapping to the
    /// correct first-day-of-week. Falls back to the current week when the
    /// value is missing or unparseable, so a mangled URL never lands the user
    /// on a blank page.
    /// </summary>
    public static DateOnly ParseWeekStart(string? value) =>
        DateOnly.TryParse(value, out var parsed) ? StartOfWeek(parsed) : CurrentWeekStart();
}
