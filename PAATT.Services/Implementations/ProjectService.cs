using Microsoft.EntityFrameworkCore;
using PAATT.Data;
using PAATT.Data.Entities;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;

namespace PAATT.Services.Implementations;

public sealed class ProjectService(ApplicationDbContext database) : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ProjectQuery().OrderBy(project => project.Name).Select(ProjectExpression()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectDto>> GetManagedByAsync(string managerId, CancellationToken cancellationToken = default) =>
        await ProjectQuery().Where(project => project.ProjectManagerId == managerId).OrderBy(project => project.Name).Select(ProjectExpression()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectDto>> GetAssignedToAsync(string consultantId, CancellationToken cancellationToken = default) =>
        await ProjectQuery().Where(project => project.ProjectAssignments.Any(assignment => assignment.ConsultantId == consultantId)).OrderBy(project => project.Name).Select(ProjectExpression()).ToListAsync(cancellationToken);

    public async Task<ProjectDto> CreateAsync(CreateProjectDto request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, cancellationToken);
        var project = new Project();
        Apply(project, request);
        database.Projects.Add(project);
        await database.SaveChangesAsync(cancellationToken);
        return await FindDtoAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto request, CancellationToken cancellationToken = default)
    {
        await ValidateRequestAsync(request, cancellationToken, id);
        var project = await database.Projects.Include(item => item.ProjectAssignments).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ApplicationValidationException("Project not found.");
        Apply(project, request);
        project.IsActive = request.IsActive;
        await database.SaveChangesAsync(cancellationToken);
        return await FindDtoAsync(id, cancellationToken);
    }

    private IQueryable<Project> ProjectQuery() => database.Projects.AsNoTracking();
    private async Task<ProjectDto> FindDtoAsync(int id, CancellationToken cancellationToken) =>
        await ProjectQuery().Where(project => project.Id == id).Select(ProjectExpression()).SingleAsync(cancellationToken);

    private static System.Linq.Expressions.Expression<Func<Project, ProjectDto>> ProjectExpression() => project => new ProjectDto(
        project.Id, project.ClientId, project.Client.Name, project.ProjectManagerId, project.ProjectManager.Name, project.Name,
        project.BudgetedHours, project.IsActive,
        project.ProjectAssignments.OrderBy(assignment => assignment.Consultant.Name).Select(assignment => new EmployeeDto(
            assignment.ConsultantId, assignment.Consultant.Name, assignment.Consultant.Email!, assignment.Consultant.IsActive, Array.Empty<string>())).ToList());

    private async Task ValidateRequestAsync(CreateProjectDto request, CancellationToken cancellationToken, int? projectId = null)
    {
        if (!await database.Clients.AnyAsync(client => client.Id == request.ClientId && client.IsActive, cancellationToken))
            throw new ApplicationValidationException("Select an active client.");
        if (!await database.Users.AnyAsync(user => user.Id == request.ProjectManagerId && user.IsActive, cancellationToken))
            throw new ApplicationValidationException("Select an active project manager.");
        var consultantIds = request.ConsultantIds.Distinct().ToArray();
        if (consultantIds.Contains(request.ProjectManagerId))
            throw new ApplicationValidationException("A project manager cannot also be a consultant on the same project.");
        var consultantCount = await database.Users.CountAsync(user => consultantIds.Contains(user.Id) && user.IsActive, cancellationToken);
        if (consultantCount != consultantIds.Length)
            throw new ApplicationValidationException("One or more consultants are invalid or inactive.");
        var name = request.Name.Trim();
        var duplicate = database.Projects.Where(project => project.ClientId == request.ClientId && project.Name == name);
        if (projectId.HasValue)
            duplicate = duplicate.Where(project => project.Id != projectId.Value);
        if (await duplicate.AnyAsync(cancellationToken))
            throw new ApplicationValidationException("This client already has a project with that name.");
    }

    private static void Apply(Project project, CreateProjectDto request)
    {
        project.ClientId = request.ClientId;
        project.ProjectManagerId = request.ProjectManagerId;
        project.Name = request.Name.Trim();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        project.BudgetedHours = request.BudgetedHours;
        project.ProjectAssignments.Clear();
        foreach (var consultantId in request.ConsultantIds.Distinct())
            project.ProjectAssignments.Add(new ProjectAssignment { ConsultantId = consultantId });
    }
}
