using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeeService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<EmployeeDto?> GetByIdAsync(string employeeId)
    {
        var user = await _db.Users
            .Include(u => u.ProjectAssignments).ThenInclude(pa => pa.Project)
            .FirstOrDefaultAsync(u => u.Id == employeeId);

        if (user is null) return null;

        var isAdmin = await _userManager.IsInRoleAsync(user, SeedData.AdminRole);
        return ToDto(user, isAdmin);
    }

    public async Task<List<EmployeeDto>> GetAllAsync()
    {
        var users = await _db.Users
            .Include(u => u.ProjectAssignments).ThenInclude(pa => pa.Project)
            .OrderBy(u => u.Name)
            .ToListAsync();

        // One role-membership query for everyone, instead of one per user.
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        var adminIds = admins.Select(a => a.Id).ToHashSet();

        return users.Select(u => ToDto(u, adminIds.Contains(u.Id))).ToList();
    }

    public async Task<List<EmployeeDto>> GetManagersAsync()
    {
        var all = await GetAllAsync();
        return all.Where(e => e.ManagedProjectNames.Count > 0)
                   .OrderBy(e => e.Name)
                   .ToList();
    }

    public async Task<List<EmployeeDto>> GetConsultantsAsync()
    {
        var all = await GetAllAsync();
        return all.Where(e => e.ConsultantProjectNames.Count > 0)
                   .OrderBy(e => e.Name)
                   .ToList();
    }

    //public async Task<List<EmployeeDto>> GetAdminsAsync()
    //{
    //    var all = await GetAllAsync();
    //    return all.Where(e => e.IsAdmin)
    //               .OrderBy(e => e.Name)
    //               .ToList();
    //}

    public async Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<EmployeeDto>.Fail("Name is required.");

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return ServiceResult<EmployeeDto>.Fail("An employee with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Name = name.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return ServiceResult<EmployeeDto>.Fail(createResult.Errors.Select(e => e.Description));

        // Everyone is a Consultant by default — Manager/Admin come later, on
        // top of this, never in place of it. Never grant Admin here.
        await _userManager.AddToRoleAsync(user, SeedData.ConsultantRole);

        return ServiceResult<EmployeeDto>.Ok(ToDto(user, isAdmin: false));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(string employeeId, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<EmployeeDto>.Fail("Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return ServiceResult<EmployeeDto>.Fail("Email is required.");

        var user = await _userManager.FindByIdAsync(employeeId);
        if (user is null)
            return ServiceResult<EmployeeDto>.Fail("Employee not found.");

        email = email.Trim();

        var clash = await _userManager.FindByEmailAsync(email);
        if (clash is not null && clash.Id != user.Id)
            return ServiceResult<EmployeeDto>.Fail("Another employee already uses this email.");

        user.Name = name.Trim();

        // Email is also the login (UserName), so the two move together.
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmail = await _userManager.SetEmailAsync(user, email);
            if (!setEmail.Succeeded)
                return ServiceResult<EmployeeDto>.Fail(setEmail.Errors.Select(e => e.Description));

            var setUserName = await _userManager.SetUserNameAsync(user, email);
            if (!setUserName.Succeeded)
                return ServiceResult<EmployeeDto>.Fail(setUserName.Errors.Select(e => e.Description));

            // Admin-created accounts are trusted; keep them confirmed so the
            // user isn't locked out by the email change.
            user.EmailConfirmed = true;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return ServiceResult<EmployeeDto>.Fail(updateResult.Errors.Select(e => e.Description));

        var refreshed = await GetByIdAsync(user.Id);
        return ServiceResult<EmployeeDto>.Ok(refreshed!);
    }

    public async Task<ServiceResult> DeleteEmployeeAsync(string employeeId)
    {
        var user = await _db.Users
            .Include(u => u.ProjectAssignments)
            .Include(u => u.Timesheets)
            .FirstOrDefaultAsync(u => u.Id == employeeId);

        if (user is null)
            return ServiceResult.Fail("Employee not found.");

        // Assignments and timesheets both point at this user; removing the
        // user would either cascade away real history or break the FK.
        // Make the Admin deal with it explicitly instead.
        if (user.ProjectAssignments.Count > 0)
            return ServiceResult.Fail("Cannot delete someone who still holds project assignments. Remove their assignments first.");

        if (user.Timesheets.Count > 0)
            return ServiceResult.Fail("Cannot delete someone who has timesheets — their logged time would be lost.");

        var isAdmin = await _userManager.IsInRoleAsync(user, SeedData.AdminRole);
        if (isAdmin)
        {
            var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
            if (admins.Count <= 1)
                return ServiceResult.Fail("Cannot delete the last remaining Admin.");
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            return ServiceResult.Fail(deleteResult.Errors.Select(e => e.Description));

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetAdminAsync(string employeeId, bool isAdmin)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId);
        if (user is null)
            return ServiceResult.Fail("Employee not found.");

        var currentlyAdmin = await _userManager.IsInRoleAsync(user, SeedData.AdminRole);
        if (currentlyAdmin == isAdmin)
            return ServiceResult.Ok(); // no-op, already in the requested state

        if (!isAdmin)
        {
            // Guard against revoking the last remaining Admin and locking
            // everyone out of the Admin portal.
            var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
            if (admins.Count <= 1)
                return ServiceResult.Fail("Cannot remove Admin from the last remaining Admin.");

            var removeResult = await _userManager.RemoveFromRoleAsync(user, SeedData.AdminRole);
            if (!removeResult.Succeeded)
                return ServiceResult.Fail(removeResult.Errors.Select(e => e.Description));
        }
        else
        {
            // Promoting to Admin never touches the Consultant role — it's
            // additive, so the person can still be assigned to projects.
            var addResult = await _userManager.AddToRoleAsync(user, SeedData.AdminRole);
            if (!addResult.Succeeded)
                return ServiceResult.Fail(addResult.Errors.Select(e => e.Description));
        }

        return ServiceResult.Ok();
    }

    private static EmployeeDto ToDto(ApplicationUser user, bool isAdmin) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email ?? string.Empty,
        IsAdmin = isAdmin,
        ManagedProjectNames = user.ProjectAssignments
            .Where(pa => pa.AssignmentType == AssignmentType.Manager)
            .Select(pa => pa.Project.Name)
            .OrderBy(n => n)
            .ToList(),
        ConsultantProjectNames = user.ProjectAssignments
            .Where(pa => pa.AssignmentType == AssignmentType.Consultant)
            .Select(pa => pa.Project.Name)
            .OrderBy(n => n)
            .ToList()
    };
}
