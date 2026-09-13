using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PAATT.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ClientApiService>();
builder.Services.AddScoped<EmployeeApiService>();
builder.Services.AddScoped<ProjectApiService>();
builder.Services.AddScoped<TimesheetApiService>();
builder.Services.AddScoped<AccountApiService>();

await builder.Build().RunAsync();
