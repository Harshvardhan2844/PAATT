using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PAATT.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
