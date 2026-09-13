using PAATT.Shared.DTOs;

namespace PAATT.Services.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetManagedByAsync(string managerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetAssignedToAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectDto request, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto request, CancellationToken cancellationToken = default);
}
