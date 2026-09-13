using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/clients"), Authorize(Roles = nameof(AppRole.Admin))]
public sealed class ClientsController(IClientService service) : ApiControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ClientDto>> Get(CancellationToken token) => service.GetAllAsync(token);
    [HttpPost] public async Task<ActionResult<ClientDto>> Create(CreateClientDto request, CancellationToken token) => Created("", await service.CreateAsync(request, token));
    [HttpPut("{id:int}")] public Task<ClientDto> Update(int id, UpdateClientDto request, CancellationToken token) => service.UpdateAsync(id, request, token);
}
