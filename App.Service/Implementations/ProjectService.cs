using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _db;

    public ProjectService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProjectDto>> GetAllAsync()
    {
        var projects = await _db.Projects
            .Include(p => p.Client)
            .Include(p => p.Assignments).ThenInclude(a => a.Employee)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Client)
            .Include(p => p.Assignments).ThenInclude(a => a.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);

        return project is null ? null : ToDto(project);
    }

    public async Task<ServiceResult<ProjectDto>> CreateAsync(string name, int clientId, bool isBillable)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<ProjectDto>.Fail("Project name is required.");

        var clientExists = await _db.Clients.AnyAsync(c => c.Id == clientId);
        if (!clientExists)
            return ServiceResult<ProjectDto>.Fail("Selected client does not exist.");

        var project = new Project { Name = name.Trim(), ClientId = clientId, IsBillable = isBillable };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var client = await _db.Clients.FindAsync(clientId);
        return ServiceResult<ProjectDto>.Ok(new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            ClientId = project.ClientId,
            ClientName = client?.CompanyName ?? string.Empty,
            IsBillable = project.IsBillable,
            ManagerName = null,
            ConsultantCount = 0
        });
    }

    public async Task<ServiceResult<ProjectDto>> UpdateAsync(int id, string name, int clientId, bool isBillable)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<ProjectDto>.Fail("Project name is required.");

        var project = await _db.Projects.FindAsync(id);
        if (project is null)
            return ServiceResult<ProjectDto>.Fail("Project not found.");

        var clientExists = await _db.Clients.AnyAsync(c => c.Id == clientId);
        if (!clientExists)
            return ServiceResult<ProjectDto>.Fail("Selected client does not exist.");

        project.Name = name.Trim();
        project.ClientId = clientId;
        project.IsBillable = isBillable;
        await _db.SaveChangesAsync();

        var updated = await GetByIdAsync(id);
        return ServiceResult<ProjectDto>.Ok(updated!);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var project = await _db.Projects
            .Include(p => p.Assignments)
            .Include(p => p.Timesheets)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
            return ServiceResult.Fail("Project not found.");

        if (project.Assignments.Count > 0 || project.Timesheets.Count > 0)
            return ServiceResult.Fail("Cannot delete a project that has assignments or timesheets. Remove those first.");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<List<ProjectDto>> GetProjectsManagedByAsync(string employeeId)
    {
        var projects = await _db.Projects
            .Include(p => p.Client)
            .Include(p => p.Assignments).ThenInclude(a => a.Employee)
            .Where(p => p.Assignments.Any(a => a.EmployeeId == employeeId && a.AssignmentType == AssignmentType.Manager))
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(ToDto).ToList();
    }

    public async Task<List<ProjectDto>> GetProjectsConsultedOnByAsync(string employeeId)
    {
        var projects = await _db.Projects
            .Include(p => p.Client)
            .Include(p => p.Assignments).ThenInclude(a => a.Employee)
            .Where(p => p.Assignments.Any(a => a.EmployeeId == employeeId && a.AssignmentType == AssignmentType.Consultant))
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(ToDto).ToList();
    }

    private static ProjectDto ToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClientId = p.ClientId,
        ClientName = p.Client?.CompanyName ?? string.Empty,
        IsBillable = p.IsBillable,
        ManagerName = p.Assignments.FirstOrDefault(a => a.AssignmentType == AssignmentType.Manager)?.Employee.Name,
        ConsultantCount = p.Assignments.Count(a => a.AssignmentType == AssignmentType.Consultant)
    };
}
