using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAATT.Data.Entities;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Services.Implementations;

public sealed class EmployeeService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.OrderBy(user => user.Name).ToListAsync(cancellationToken);
        var result = new List<EmployeeDto>(users.Count);
        foreach (var user in users)
            result.Add(await ToDtoAsync(user));
        return result;
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser { Name = request.Name.Trim(), Email = request.Email.Trim(), UserName = request.Email.Trim(), EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, request.Password);
        ThrowWhenFailed(result);

        var roles = request.Roles?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [AppRole.Consultant.ToString()];
        await AssignRolesAsync(user, roles);
        return await ToDtoAsync(user);
    }

    public async Task<EmployeeDto> UpdateRolesAsync(string userId, UpdateEmployeeRolesDto request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new ApplicationValidationException("Employee not found.");
        var requestedRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        ValidateKnownRoles(requestedRoles);
        var currentRoles = await userManager.GetRolesAsync(user);
        ThrowWhenFailed(await userManager.RemoveFromRolesAsync(user, currentRoles));
        await AssignRolesAsync(user, requestedRoles);
        return await ToDtoAsync(user);
    }

    private async Task AssignRolesAsync(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        ValidateKnownRoles(roles);
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                throw new ApplicationValidationException($"The role '{role}' is not available.");
        ThrowWhenFailed(await userManager.AddToRolesAsync(user, roles));
    }

    private async Task<EmployeeDto> ToDtoAsync(ApplicationUser user) => new(user.Id, user.Name, user.Email ?? string.Empty, user.IsActive, (await userManager.GetRolesAsync(user)).Order().ToArray());
    private static void ValidateKnownRoles(IEnumerable<string> roles)
    {
        var allowed = Enum.GetNames<AppRole>();
        if (roles.Any(role => !allowed.Contains(role, StringComparer.OrdinalIgnoreCase)))
            throw new ApplicationValidationException("One or more requested roles are invalid.");
    }
    private static void ThrowWhenFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ApplicationValidationException(string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
