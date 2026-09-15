using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;

namespace App.WebClient.Services;

/// <summary>
/// The single surface the Admin Portal pages talk to. There are two
/// implementations, because the portal runs under InteractiveAuto:
///
///  - ServerAdminApi (in App.Web) calls the App.Service layer directly. Used
///    during prerender and while the circuit is running on the server, so
///    those renders don't make a pointless HTTP call to the app itself.
///  - HttpAdminApi (here) calls the /api/admin endpoints over HTTP. Used once
///    the component has downloaded and switched to WebAssembly, where there
///    is no DbContext to talk to.
///
/// Pages never know which one they got. Every method that can fail a business
/// rule returns ServiceResult so the UI can render the message instead of
/// catching an exception.
/// </summary>
public interface IAdminApi
{
    // ---------- Employees ----------

    Task<List<EmployeeDto>> GetEmployeesAsync();

    /// <summary>Everyone holding at least one Manager-type assignment, sorted by name.</summary>
    Task<List<EmployeeDto>> GetManagersAsync();

    /// <summary>Everyone holding at least one Consultant-type assignment, sorted by name.</summary>
    Task<List<EmployeeDto>> GetConsultantsAsync();

    Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request);

    Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(string employeeId, UpdateEmployeeRequest request);

    /// <summary>Grants or revokes the Admin role. Consultant role is untouched either way.</summary>
    Task<ServiceResult> SetAdminAsync(string employeeId, bool isAdmin);

    Task<ServiceResult> DeleteEmployeeAsync(string employeeId);

    // ---------- Clients ----------

    Task<List<ClientDto>> GetClientsAsync();

    Task<ServiceResult<ClientDto>> CreateClientAsync(ClientRequest request);

    Task<ServiceResult<ClientDto>> UpdateClientAsync(int clientId, ClientRequest request);

    Task<ServiceResult> DeleteClientAsync(int clientId);

    // ---------- Projects ----------

    Task<List<ProjectDto>> GetProjectsAsync();

    Task<ProjectDto?> GetProjectAsync(int projectId);

    Task<ServiceResult<ProjectDto>> CreateProjectAsync(ProjectRequest request);

    Task<ServiceResult<ProjectDto>> UpdateProjectAsync(int projectId, ProjectRequest request);

    Task<ServiceResult> DeleteProjectAsync(int projectId);

    // ---------- Assignments ----------

    Task<List<ProjectAssignmentDto>> GetAssignmentsForProjectAsync(int projectId);

    /// <summary>
    /// Creates or updates the (project, employee) assignment row. The service
    /// layer rejects a second Manager on the project, and requires budgeted
    /// hours on Consultant rows.
    /// </summary>
    Task<ServiceResult<ProjectAssignmentDto>> AssignAsync(AssignmentRequest request);

    Task<ServiceResult> RemoveAssignmentAsync(int assignmentId);
}
