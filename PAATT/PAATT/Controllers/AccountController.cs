using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PAATT.Data.Entities;
using PAATT.Shared.DTOs;

namespace PAATT.Controllers;

[Route("api/account"), Authorize]
public sealed class AccountController(UserManager<ApplicationUser> userManager) : ApiControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto request, [FromServices] SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive) return Unauthorized(new { message = "Invalid email or password." });
        var result = await signInManager.PasswordSignInAsync(user, request.Password, false, false);
        return result.Succeeded ? Ok() : Unauthorized(new { message = "Invalid email or password." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromServices] SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);
        if (user is null) return Unauthorized();
        return new CurrentUserDto(user.Id, user.Name, (await userManager.GetRolesAsync(user)).ToArray());
    }
}
