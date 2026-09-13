using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/project-assignments"), Authorize(Roles = nameof(AppRole.Admin))]
public sealed class ProjectAssignmentsController(IProjectAssignmentService service) : ApiControllerBase
{
    [HttpPost] public async Task<ActionResult<ProjectAssignmentDto>> Create(CreateProjectAssignmentDto request, CancellationToken token) => Created("", await service.CreateAsync(request, token));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
}
