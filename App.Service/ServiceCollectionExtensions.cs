using App.Service.Implementations;
using App.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace App.Service;

/// <summary>
/// Optional convenience: call builder.Services.AddAppServices() from
/// App.Web's Program.cs instead of registering each service individually.
/// All services are scoped since they depend on ApplicationDbContext (also scoped).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<ITimesheetEntryService, TimesheetEntryService>();

        return services;
    }
}
