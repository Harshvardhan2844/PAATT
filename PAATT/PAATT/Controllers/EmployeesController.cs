using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/employees"), Authorize(Roles = nameof(AppRole.Admin))]
public sealed class EmployeesController(IEmployeeService service) : ApiControllerBase
{
    [HttpGet] public Task<IReadOnlyList<EmployeeDto>> Get(CancellationToken token) => service.GetAllAsync(token);
    [HttpPost] public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto request, CancellationToken token) => Created("", await service.CreateAsync(request, token));
    [HttpPut("{id}/roles")] public Task<EmployeeDto> Roles(string id, UpdateEmployeeRolesDto request, CancellationToken token) => service.UpdateRolesAsync(id, request, token);
}
