using App.Shared;
using App.Shared.Dtos;

namespace App.Service.Interfaces;

/// <summary>
/// Everyone is created plain (Identity Role = "Consultant", no role picker
/// at creation time) — whether someone shows up as a Manager or Consultant
/// on a given project is entirely derived from their ProjectAssignment rows,
/// computed here, not stored on the user. The Consultant role itself is
/// permanent and never removed.
///
/// Separately, an Admin can grant or revoke the "Admin" Identity role on top
/// of any existing Consultant via SetAdminAsync — the role is not fixed at
/// creation, it can change over time, and Admin always stacks on top of
/// Consultant rather than replacing it.
/// </summary>
public interface IEmployeeService
{
    Task<EmployeeDto?> GetByIdAsync(string employeeId);

    Task<List<EmployeeDto>> GetAllAsync();

    /// <summary>Everyone with at least one Manager-type assignment, sorted by name.</summary>
    Task<List<EmployeeDto>> GetManagersAsync();

    /// <summary>Everyone with at least one Consultant-type assignment, sorted by name.</summary>
    Task<List<EmployeeDto>> GetConsultantsAsync();

    /// <summary>Everyone currently holding the additional Admin role, sorted by name.</summary>
    Task<List<EmployeeDto>> GetAdminsAsync();

    /// <summary>Creates a new user (Admin action). Always gets the Consultant role; never Admin at creation. Fails if the email is already taken.</summary>
    Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(string name, string email, string password);

    /// <summary>
    /// Admin action: updates display name and email. Email is also the
    /// Identity UserName, so this changes their login. Fails if another user
    /// already holds that email. Added in Phase 3 — the Admin Portal's
    /// "Employee CRUD" needs an update path that Phase 2 didn't provide.
    /// </summary>
    Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(string employeeId, string name, string email);

    /// <summary>
    /// Admin action: permanently deletes a user. Refuses when the person
    /// still holds project assignments or owns timesheets (deleting would
    /// destroy time history), and refuses to delete the last remaining Admin.
    /// Added in Phase 3 alongside UpdateEmployeeAsync.
    /// </summary>
    Task<ServiceResult> DeleteEmployeeAsync(string employeeId);

    /// <summary>
    /// Admin action: grants (true) or revokes (false) the Admin role for an
    /// existing user. The person keeps their Consultant role either way — 
    /// Admin is additive, not a replacement. Fails if this would revoke Admin
    /// from the last remaining Admin, to avoid locking everyone out.
    /// </summary>
    Task<ServiceResult> SetAdminAsync(string employeeId, bool isAdmin);
}
