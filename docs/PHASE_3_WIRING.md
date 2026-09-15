# Phase 3 — wiring notes

Everything below is glue that belongs in the scaffold you own. The code files in this drop compile
against it but don't create it.

## 1. Project references

```
App.Web       → App.WebClient, App.Service, App.Data, App.Shared
App.WebClient → App.Shared
App.Service   → App.Data, App.Shared
App.Data      → App.Shared
```

`App.Web → App.WebClient` is the one worth checking. It's already there in the standard Blazor Web App
Auto template, and Phase 3 relies on it: `IAdminApi` lives in `App.WebClient/Services` and `ServerAdminApi`
in `App.Web` implements it.

## 2. App.Web/Program.cs

```csharp
using App.Service;                 // AddAppServices()
using App.Web.Endpoints;           // MapAdminApi()
using App.Web.Services;            // ServerAdminApi
using App.WebClient.Services;      // IAdminApi

// --- services ---
builder.Services.AddAppServices();                          // Phase 2 helper
builder.Services.AddScoped<IAdminApi, ServerAdminApi>();    // server-side implementation

builder.Services.AddCascadingAuthenticationState();         // needed by [Authorize] + the "you" badge

// --- pipeline (order matters) ---
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAdminApi();                                          // /api/admin/*
```

`MapAdminApi()` has to come after `UseAuthentication`/`UseAuthorization`, or the role check has nothing to
check. The endpoints take JSON bodies rather than form posts, so `UseAntiforgery()` doesn't apply to them —
no token plumbing needed in `HttpAdminApi`.

Also make sure `Routes.razor` still passes the client assembly, which the template does by default:

```razor
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(App.WebClient._Imports).Assembly }">
```

Without it the `/admin/*` routes 404, because every page in this drop lives in `App.WebClient`.

## 3. App.WebClient/Program.cs

```csharp
using App.WebClient.Services;

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<IAdminApi, HttpAdminApi>();      // WebAssembly-side implementation

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
```

Same-origin requests send the auth cookie automatically, so there's nothing else to configure. If you ever
host the API on a different origin, you'd need `BrowserRequestCredentials.Include` on the handler.

## 4. Auth state on the WebAssembly side

`[Authorize(Roles = "Admin")]` on a page only works client-side if the client knows who's signed in. The
`dotnet new blazor -au Individual -int Auto` template generates that plumbing for you:

- `App.Web/PersistingRevalidatingAuthenticationStateProvider.cs`
- `App.WebClient/PersistentAuthenticationStateProvider.cs`
- `App.WebClient/UserInfo.cs`

If your scaffold has those, Phase 3 needs nothing further. If you scaffolded without Individual Accounts,
add them before the role checks will behave — otherwise the pages render as unauthorised after the switch
to WebAssembly, even though the server-side endpoints stay correctly protected.

The `UserInfo` the template persists must include the role claim and the name-identifier claim. The
Employees screen reads `ClaimTypes.NameIdentifier` to grey out "delete yourself" and "demote yourself".
If your `UserInfo` only carries email, that guard silently does nothing (the service-layer
"last remaining Admin" check still holds).

## 5. Nav links

Wherever your `NavMenu.razor` lives, the four screens are:

```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="admin/employees">Employees</NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="admin/clients">Clients</NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="admin/projects">Projects</NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="admin/assignments">Project assignments</NavLink>
        </div>
    </Authorized>
</AuthorizeView>
```

`/admin/assigned-employees` is reachable from the Employees screen, so it doesn't need its own nav entry
unless you want one.

## 6. _Imports.razor

`App.WebClient/_Imports.razor` in this drop adds `@using static Microsoft.AspNetCore.Components.Web.RenderMode`,
which is what lets the pages write `@rendermode InteractiveAuto`. If you already have an `_Imports.razor`,
merge rather than overwrite — compare it against the one here and take the `App.Shared.*`,
`App.WebClient.*` and `RenderMode` lines.

## 7. Styling

Bootstrap 5 classes throughout, matching the template's default stylesheet. No custom CSS, no JS interop —
the delete confirmations are two-click buttons rather than `confirm()` dialogs, so they behave the same
prerendered and interactive. Phase 6 is where a real styling pass belongs.
