using App.Data;
using App.Shared.Dtos.Requests;
using App.Shared.Enums;
using App.WebClient.Services;

namespace App.Web.Endpoints;

/// <summary>
/// The HTTP surface for the Project Manager Portal.
///
/// The role requirement here is Consultant, which reads oddly until you
/// remember the confirmed model: there is no ProjectManager Identity role.
/// Everyone is a Consultant, and being a manager is a per-project assignment.
/// So the route-level check only proves the caller is a signed-in user of this
/// app; the real check — do they manage *this* project — happens in
/// App.Service on every one of these calls.
///
/// Call app.MapManagerApi() from Program.cs, after UseAuthentication/
/// UseAuthorization.
/// </summary>
public static class ManagerEndpoints
{
    public static IEndpointRouteBuilder MapManagerApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/manager")
            .RequireAuthorization(policy => policy.RequireRole(SeedData.ConsultantRole));

        group.MapGet("/projects", async (IManagerApi api) =>
            Results.Ok(await api.GetMyProjectsAsync()));

        group.MapGet("/projects/{projectId:int}", async (int projectId, IManagerApi api) =>
            await api.GetProjectAsync(projectId) is { } project
                ? Results.Ok(project)
                : Results.NotFound());

        group.MapGet("/projects/{projectId:int}/budgets", async (int projectId, IManagerApi api) =>
            Results.Ok(await api.GetProjectBudgetsAsync(projectId)));

        group.MapGet("/timesheets", async (TimesheetStatus? status, IManagerApi api) =>
            Results.Ok(await api.GetQueueAsync(status)));

        group.MapGet("/timesheets/{timesheetId:int}", async (int timesheetId, IManagerApi api) =>
            await api.GetTimesheetAsync(timesheetId) is { } timesheet
                ? Results.Ok(timesheet)
                : Results.NotFound());

        group.MapGet("/timesheets/{timesheetId:int}/history", async (int timesheetId, IManagerApi api) =>
            Results.Ok(await api.GetHistoryAsync(timesheetId)));

        group.MapPost("/timesheets/{timesheetId:int}/approve", async (int timesheetId, IManagerApi api) =>
            Results.Ok(await api.ApproveTimesheetAsync(timesheetId)));

        group.MapPost("/timesheets/{timesheetId:int}/reject", async (int timesheetId, RejectionRequest request, IManagerApi api) =>
            Results.Ok(await api.RejectTimesheetAsync(timesheetId, request)));

        group.MapPost("/entries/{entryId:int}/reject", async (int entryId, RejectionRequest request, IManagerApi api) =>
            Results.Ok(await api.RejectEntryAsync(entryId, request)));

        return endpoints;
    }
}
