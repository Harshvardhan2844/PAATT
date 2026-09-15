using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;
using App.WebClient.Services;

namespace App.Web.Services;

/// <summary>
/// Server-side implementation of IAdminApi. Calls the App.Service layer
/// directly — no HTTP hop — so prerendered and server-interactive renders of
/// the Admin Portal hit the database straight away.
///
/// This is also what the /api/admin endpoints delegate to (see
/// AdminEndpoints), so the mapping between "what the UI can ask for" and
/// "what the service layer does" lives in exactly one place. Business rules
/// stay in App.Service; this type only translates shapes.
/// </summary>
public class ServerAdminApi : IAdminApi
{
    private readonly IEmployeeService _employees;
    private readonly IClientService _clients;
    private readonly IProjectService _projects;
    private readonly IProjectAssignmentService _assignments;

    public ServerAdminApi(
        IEmployeeService employees,
        IClientService clients,
        IProjectService projects,
        IProjectAssignmentService assignments)
    {
        _employees = employees;
        _clients = clients;
        _projects = projects;
        _assignments = assignments;
    }

    // ---------- Employees ----------

    public Task<List<EmployeeDto>> GetEmployeesAsync() => _employees.GetAllAsync();

    public Task<List<EmployeeDto>> GetManagersAsync() => _employees.GetManagersAsync();

    public Task<List<EmployeeDto>> GetConsultantsAsync() => _employees.GetConsultantsAsync();

    public Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request)
        => _employees.CreateEmployeeAsync(request.Name, request.Email, request.Password);

    public Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(string employeeId, UpdateEmployeeRequest request)
        => _employees.UpdateEmployeeAsync(employeeId, request.Name, request.Email);

    public Task<ServiceResult> SetAdminAsync(string employeeId, bool isAdmin)
        => _employees.SetAdminAsync(employeeId, isAdmin);

    public Task<ServiceResult> DeleteEmployeeAsync(string employeeId)
        => _employees.DeleteEmployeeAsync(employeeId);

    // ---------- Clients ----------

    public Task<List<ClientDto>> GetClientsAsync() => _clients.GetAllAsync();

    public Task<ServiceResult<ClientDto>> CreateClientAsync(ClientRequest request)
        => _clients.CreateAsync(request.CompanyName);

    public Task<ServiceResult<ClientDto>> UpdateClientAsync(int clientId, ClientRequest request)
        => _clients.UpdateAsync(clientId, request.CompanyName);

    public Task<ServiceResult> DeleteClientAsync(int clientId) => _clients.DeleteAsync(clientId);

    // ---------- Projects ----------

    public Task<List<ProjectDto>> GetProjectsAsync() => _projects.GetAllAsync();

    public Task<ProjectDto?> GetProjectAsync(int projectId) => _projects.GetByIdAsync(projectId);

    public Task<ServiceResult<ProjectDto>> CreateProjectAsync(ProjectRequest request)
        => _projects.CreateAsync(request.Name, request.ClientId, request.IsBillable);

    public Task<ServiceResult<ProjectDto>> UpdateProjectAsync(int projectId, ProjectRequest request)
        => _projects.UpdateAsync(projectId, request.Name, request.ClientId, request.IsBillable);

    public Task<ServiceResult> DeleteProjectAsync(int projectId) => _projects.DeleteAsync(projectId);

    // ---------- Assignments ----------

    public Task<List<ProjectAssignmentDto>> GetAssignmentsForProjectAsync(int projectId)
        => _assignments.GetAssignmentsForProjectAsync(projectId);

    public Task<ServiceResult<ProjectAssignmentDto>> AssignAsync(AssignmentRequest request)
        => _assignments.AssignAsync(request.ProjectId, request.EmployeeId, request.AssignmentType, request.BudgetedHours);

    public Task<ServiceResult> RemoveAssignmentAsync(int assignmentId)
        => _assignments.RemoveAssignmentAsync(assignmentId);
}
