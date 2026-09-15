using App.Shared;
using App.Shared.Dtos;

namespace App.Service.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllAsync();

    Task<ProjectDto?> GetByIdAsync(int id);

    Task<ServiceResult<ProjectDto>> CreateAsync(string name, int clientId, bool isBillable);

    Task<ServiceResult<ProjectDto>> UpdateAsync(int id, string name, int clientId, bool isBillable);

    /// <summary>Fails if the project has any assignments or timesheets — remove those first.</summary>
    Task<ServiceResult> DeleteAsync(int id);

    /// <summary>
    /// Projects where this employee holds a Manager assignment. Backs the PM
    /// portal's "only sees the project(s) assigned to them" rule.
    /// </summary>
    Task<List<ProjectDto>> GetProjectsManagedByAsync(string employeeId);

    /// <summary>
    /// Projects where this employee holds a Consultant assignment. Backs the
    /// Consultant portal's "only sees projects assigned to them" rule, and
    /// is the source for assignment-scoped project dropdowns when creating
    /// a timesheet.
    /// </summary>
    Task<List<ProjectDto>> GetProjectsConsultedOnByAsync(string employeeId);
}
