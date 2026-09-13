using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/projects"), Authorize]
public sealed class ProjectsController(IProjectService service) : ApiControllerBase
{
    [HttpGet, Authorize(Roles = nameof(AppRole.Admin))] public Task<IReadOnlyList<ProjectDto>> Get(CancellationToken token) => service.GetAllAsync(token);
    [HttpGet("my-managed"), Authorize(Roles = nameof(AppRole.ProjectManager))] public Task<IReadOnlyList<ProjectDto>> Managed(CancellationToken token) => service.GetManagedByAsync(CurrentUserId, token);
    [HttpGet("my"), Authorize(Roles = nameof(AppRole.Consultant))] public Task<IReadOnlyList<ProjectDto>> Mine(CancellationToken token) => service.GetAssignedToAsync(CurrentUserId, token);
    [HttpPost, Authorize(Roles = nameof(AppRole.Admin))] public async Task<ActionResult<ProjectDto>> Create(CreateProjectDto request, CancellationToken token) => Created("", await service.CreateAsync(request, token));
    [HttpPut("{id:int}"), Authorize(Roles = nameof(AppRole.Admin))] public Task<ProjectDto> Update(int id, UpdateProjectDto request, CancellationToken token) => service.UpdateAsync(id, request, token);
}
