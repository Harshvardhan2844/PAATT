using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using App.WebClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddScoped<IAdminApi, HttpAdminApi>();
builder.Services.AddScoped<IConsultantApi, HttpConsultantApi>();
builder.Services.AddScoped<IManagerApi, HttpManagerApi>();
builder.Services.AddScoped<RoleViewState>();

await builder.Build().RunAsync();
