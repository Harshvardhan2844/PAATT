using App.Data;
using App.Shared.Dtos.Requests;
using App.WebClient.Services;

namespace App.Web.Endpoints;

/// <summary>
/// The HTTP surface the Consultant Portal uses once it's running in the
/// browser. The whole group requires the Consultant role, which every user
/// has — the meaningful protection isn't the role, it's that no route takes
/// an employee id. ServerConsultantApi resolves the caller and checks
/// ownership; these handlers only route.
///
/// Call app.MapConsultantApi() from Program.cs, after UseAuthentication/
/// UseAuthorization.
/// </summary>
public static class ConsultantEndpoints
{
    public static IEndpointRouteBuilder MapConsultantApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/consultant")
            .RequireAuthorization(policy => policy.RequireRole(SeedData.ConsultantRole));

        group.MapGet("/projects", async (IConsultantApi api) =>
            Results.Ok(await api.GetMyProjectsAsync()));

        group.MapGet("/timesheets", async (int? projectId, IConsultantApi api) =>
            Results.Ok(await api.GetMyTimesheetsAsync(projectId)));

        // Returns 204 rather than 404 when the week has no timesheet yet:
        // "you haven't logged anything" is a normal state, not an error.
        group.MapGet("/timesheets/week", async (int projectId, DateOnly weekStarting, IConsultantApi api) =>
            await api.GetTimesheetForWeekAsync(projectId, weekStarting) is { } timesheet
                ? Results.Ok(timesheet)
                : Results.NoContent());

        group.MapGet("/daily-totals", async (DateOnly weekStarting, IConsultantApi api) =>
            Results.Ok(await api.GetDailyTotalsAsync(weekStarting)));

        group.MapPost("/entries", async (AddEntryRequest request, IConsultantApi api) =>
            Results.Ok(await api.AddEntryAsync(request)));

        group.MapPut("/entries/{entryId:int}", async (int entryId, UpdateEntryRequest request, IConsultantApi api) =>
            Results.Ok(await api.UpdateEntryAsync(entryId, request)));

        group.MapDelete("/entries/{entryId:int}", async (int entryId, IConsultantApi api) =>
            Results.Ok(await api.DeleteEntryAsync(entryId)));

        group.MapPost("/timesheets/{timesheetId:int}/submit", async (int timesheetId, IConsultantApi api) =>
            Results.Ok(await api.SubmitTimesheetAsync(timesheetId)));

        return endpoints;
    }
}
