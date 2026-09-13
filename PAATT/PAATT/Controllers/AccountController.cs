using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PAATT.Data.Entities;
using PAATT.Shared.DTOs;

namespace PAATT.Controllers;

[Route("api/account"), Authorize]
public sealed class AccountController(UserManager<ApplicationUser> userManager) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null) return Unauthorized();
        return new CurrentUserDto(user.Id, user.Name, (await userManager.GetRolesAsync(user)).ToArray());
    }
}
