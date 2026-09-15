using App.Data;
using App.Shared.Dtos.Requests;
using App.WebClient.Services;

namespace App.Web.Endpoints;

/// <summary>
/// The HTTP surface the Admin Portal uses once it has switched to
/// WebAssembly. Every route is behind the Admin role — the client-side
/// [Authorize(Roles = "Admin")] on the pages is only a UI convenience, this
/// is the check that actually matters.
///
/// Each handler delegates to IAdminApi (resolved as ServerAdminApi), so these
/// endpoints add routing and authorization and nothing else. Business-rule
/// failures are returned as HTTP 200 carrying ServiceResult.Success = false,
/// so the browser gets the validation text ("Budgeted hours are required for
/// a Consultant assignment", etc.) rather than a bare status code.
///
/// Call app.MapAdminApi() from Program.cs, after UseAuthentication/
/// UseAuthorization.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin")
            .RequireAuthorization(policy => policy.RequireRole(SeedData.AdminRole));

        MapEmployees(group);
        MapClients(group);
        MapProjects(group);
        MapAssignments(group);

        return endpoints;
    }

    private static void MapEmployees(RouteGroupBuilder group)
    {
        group.MapGet("/employees", async (IAdminApi api) =>
            Results.Ok(await api.GetEmployeesAsync()));

        group.MapGet("/employees/managers", async (IAdminApi api) =>
            Results.Ok(await api.GetManagersAsync()));

        group.MapGet("/employees/consultants", async (IAdminApi api) =>
            Results.Ok(await api.GetConsultantsAsync()));

        group.MapPost("/employees", async (CreateEmployeeRequest request, IAdminApi api) =>
            Results.Ok(await api.CreateEmployeeAsync(request)));

        group.MapPut("/employees/{employeeId}", async (string employeeId, UpdateEmployeeRequest request, IAdminApi api) =>
            Results.Ok(await api.UpdateEmployeeAsync(employeeId, request)));

        group.MapPost("/employees/{employeeId}/admin", async (string employeeId, SetAdminRequest request, IAdminApi api) =>
            Results.Ok(await api.SetAdminAsync(employeeId, request.IsAdmin)));

        group.MapDelete("/employees/{employeeId}", async (string employeeId, IAdminApi api) =>
            Results.Ok(await api.DeleteEmployeeAsync(employeeId)));
    }

    private static void MapClients(RouteGroupBuilder group)
    {
        group.MapGet("/clients", async (IAdminApi api) =>
            Results.Ok(await api.GetClientsAsync()));

        group.MapPost("/clients", async (ClientRequest request, IAdminApi api) =>
            Results.Ok(await api.CreateClientAsync(request)));

        group.MapPut("/clients/{clientId:int}", async (int clientId, ClientRequest request, IAdminApi api) =>
            Results.Ok(await api.UpdateClientAsync(clientId, request)));

        group.MapDelete("/clients/{clientId:int}", async (int clientId, IAdminApi api) =>
            Results.Ok(await api.DeleteClientAsync(clientId)));
    }

    private static void MapProjects(RouteGroupBuilder group)
    {
        group.MapGet("/projects", async (IAdminApi api) =>
            Results.Ok(await api.GetProjectsAsync()));

        group.MapGet("/projects/{projectId:int}", async (int projectId, IAdminApi api) =>
            await api.GetProjectAsync(projectId) is { } project
                ? Results.Ok(project)
                : Results.NotFound());

        group.MapPost("/projects", async (ProjectRequest request, IAdminApi api) =>
            Results.Ok(await api.CreateProjectAsync(request)));

        group.MapPut("/projects/{projectId:int}", async (int projectId, ProjectRequest request, IAdminApi api) =>
            Results.Ok(await api.UpdateProjectAsync(projectId, request)));

        group.MapDelete("/projects/{projectId:int}", async (int projectId, IAdminApi api) =>
            Results.Ok(await api.DeleteProjectAsync(projectId)));
    }

    private static void MapAssignments(RouteGroupBuilder group)
    {
        group.MapGet("/projects/{projectId:int}/assignments", async (int projectId, IAdminApi api) =>
            Results.Ok(await api.GetAssignmentsForProjectAsync(projectId)));

        group.MapPost("/assignments", async (AssignmentRequest request, IAdminApi api) =>
            Results.Ok(await api.AssignAsync(request)));

        group.MapDelete("/assignments/{assignmentId:int}", async (int assignmentId, IAdminApi api) =>
            Results.Ok(await api.RemoveAssignmentAsync(assignmentId)));
    }
}
