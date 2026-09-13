using PAATT.Client.Pages;
using PAATT.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PAATT.Data;
using PAATT.Data.Entities;
using PAATT.Services;
using PAATT.Services.Implementations;
using PAATT.Services.Interfaces;
using PAATT.Shared.Enums;
using PAATT.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.AddAuthorization();
builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/login");
builder.Services.AddControllers();

// The Blazor pages call PAATT.Web's own REST API through the same
// ClientApiService/ProjectApiService/etc. classes whether they are running
// in the browser (WebAssembly) or on the server (the "Server" half of
// Interactive Auto). When they run on the server, this HttpClient makes a
// real loopback HTTP call back into this same application.
//
// Two things used to be wrong with that loopback client:
//  1. Its BaseAddress was a hard-coded "https://localhost:5001", which does
//     not match this project's actual launch URLs (see launchSettings.json)
//     or any real deployment. Every server-rendered API call failed to
//     connect at all.
//  2. It was a brand-new HttpClient with no cookies, so even once the URL
//     was fixed, every call to an [Authorize] endpoint (almost all of them)
//     came back 401 Unauthorized - which is why role-gated data and the
//     NavMenu's role-based sidebar looked broken/empty on first load.
//
// The fix: build the client from the CURRENT incoming request (via
// IHttpContextAccessor), which is available for the initial render of every
// page. That gives us the real scheme/host to call (works in dev, IIS,
// Azure, containers, ...) and lets us copy the browser's ASP.NET Core
// Identity auth cookie onto the outgoing request, so the loopback call is
// authenticated as the same user.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(serviceProvider =>
{
    var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var baseAddress = httpContext is not null
        ? new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}")
        : new Uri(builder.Configuration["ApplicationUrl"] ?? "https://localhost:7218");
    var client = new HttpClient { BaseAddress = baseAddress };
    if (httpContext is not null && httpContext.Request.Headers.TryGetValue("Cookie", out var cookie))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie.ToArray());
    return client;
});
builder.Services.AddScoped<ClientApiService>();
builder.Services.AddScoped<EmployeeApiService>();
builder.Services.AddScoped<ProjectApiService>();
builder.Services.AddScoped<TimesheetApiService>();
builder.Services.AddScoped<AccountApiService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (ApplicationValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { message = exception.Message });
    }
});

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PAATT.Client._Imports).Assembly);

using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in Enum.GetNames<AppRole>())
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
    await PAATT.DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Environment.IsDevelopment());
}

app.Run();
