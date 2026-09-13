using Microsoft.EntityFrameworkCore;
using PAATT.Data;
using PAATT.Data.Entities;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;

namespace PAATT.Services.Implementations;

public sealed class ProjectAssignmentService(ApplicationDbContext database) : IProjectAssignmentService
{
    public async Task<ProjectAssignmentDto> CreateAsync(CreateProjectAssignmentDto request, CancellationToken cancellationToken = default)
    {
        var project = await database.Projects.SingleOrDefaultAsync(item => item.Id == request.ProjectId, cancellationToken) ?? throw new ApplicationValidationException("Project not found.");
        if (project.ProjectManagerId == request.ConsultantId)
            throw new ApplicationValidationException("A project manager cannot also be a consultant on the same project.");
        var consultant = await database.Users.SingleOrDefaultAsync(user => user.Id == request.ConsultantId && user.IsActive, cancellationToken) ?? throw new ApplicationValidationException("Consultant not found or inactive.");
        if (await database.ProjectAssignments.AnyAsync(item => item.ProjectId == request.ProjectId && item.ConsultantId == request.ConsultantId, cancellationToken))
            throw new ApplicationValidationException("This consultant is already assigned to the project.");
        var assignment = new ProjectAssignment { ProjectId = request.ProjectId, ConsultantId = request.ConsultantId };
        database.ProjectAssignments.Add(assignment);
        await database.SaveChangesAsync(cancellationToken);
        return new ProjectAssignmentDto(assignment.Id, assignment.ProjectId, assignment.ConsultantId, consultant.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await database.ProjectAssignments.FindAsync([id], cancellationToken) ?? throw new ApplicationValidationException("Assignment not found.");
        database.ProjectAssignments.Remove(assignment);
        await database.SaveChangesAsync(cancellationToken);
    }
}
