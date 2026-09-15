using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;

namespace App.Service.Interfaces;

/// <summary>
/// Backs the Admin's "Project Assignments" screen: pick one Manager + N
/// Consultants per project, in the same step. Enforces:
///  - at most one Manager-type row per project
///  - an employee can't hold both a Manager and a Consultant row on the same project
///  - BudgetedHours only applies to Consultant-type rows
/// </summary>
public interface IProjectAssignmentService
{
    Task<List<ProjectAssignmentDto>> GetAssignmentsForProjectAsync(int projectId);

    Task<List<ProjectAssignmentDto>> GetAssignmentsForEmployeeAsync(string employeeId);

    /// <summary>
    /// Creates or updates the assignment row for (projectId, employeeId).
    /// Rejects the call if it would put the same employee on the project
    /// under both AssignmentTypes, or would create a second Manager on the project.
    /// </summary>
    Task<ServiceResult<ProjectAssignmentDto>> AssignAsync(
        int projectId, string employeeId, AssignmentType assignmentType, decimal? budgetedHours);

    Task<ServiceResult> RemoveAssignmentAsync(int assignmentId);

    /// <summary>
    /// One row per Consultant on the project: their own budgeted hours and
    /// their own logged hours. Added in Phase 5 to back the manager's
    /// budget-vs-actual view.
    ///
    /// Note what this deliberately does not return: a project total. The
    /// confirmed rule is that a manager compares each consultant against
    /// their own budget, never against a shared pot, so there's no summed
    /// figure here for a caller to display by accident.
    /// </summary>
    Task<List<BudgetVsActualDto>> GetBudgetVsActualAsync(int projectId);
}
