using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace App.Web.Services;

/// <summary>
/// Answers "who is making this call" for code that runs in both of the places
/// App.Web executes: an ordinary HTTP request (a /api/consultant endpoint, or
/// a prerender pass) and a Blazor interactive-server circuit (no HttpContext,
/// the user lives in the Blazor authentication state instead).
///
/// The portal APIs use this instead of taking an employee id from the caller,
/// which is what stops a signed-in consultant from asking for somebody else's
/// timesheet.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>The signed-in user's Identity id, or null if nobody is signed in.</summary>
    Task<string?> GetUserIdAsync();
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _services;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, IServiceProvider services)
    {
        _httpContextAccessor = httpContextAccessor;
        _services = services;
    }

    public async Task<string?> GetUserIdAsync()
    {
        // Endpoints and prerendering: the user is on the HttpContext.
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Interactive server circuit: there's no HttpContext to read, so ask
        // Blazor. Resolved lazily rather than injected, because in a plain API
        // request scope the AuthenticationStateProvider has no state set and
        // constructing it serves no purpose.
        var provider = _services.GetService<AuthenticationStateProvider>();
        if (provider is null) return null;

        try
        {
            var state = await provider.GetAuthenticationStateAsync();
            return state.User.Identity?.IsAuthenticated == true
                ? state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                : null;
        }
        catch (InvalidOperationException)
        {
            // Provider exists but has no authentication state for this scope.
            return null;
        }
    }
}
