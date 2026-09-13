using PAATT.Shared.DTOs;

namespace PAATT.Services.Interfaces;

public interface IClientService
{
    Task<IReadOnlyList<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(CreateClientDto request, CancellationToken cancellationToken = default);
    Task<ClientDto> UpdateAsync(int id, UpdateClientDto request, CancellationToken cancellationToken = default);
}
