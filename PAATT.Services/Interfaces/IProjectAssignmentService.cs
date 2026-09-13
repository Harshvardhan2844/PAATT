using PAATT.Shared.DTOs;

namespace PAATT.Services.Interfaces;

public interface IProjectAssignmentService
{
    Task<ProjectAssignmentDto> CreateAsync(CreateProjectAssignmentDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
