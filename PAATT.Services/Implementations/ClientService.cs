using Microsoft.EntityFrameworkCore;
using PAATT.Data;
using PAATT.Data.Entities;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;

namespace PAATT.Services.Implementations;

public sealed class ClientService(ApplicationDbContext database) : IClientService
{
    public async Task<IReadOnlyList<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await database.Clients.AsNoTracking().OrderBy(client => client.Name).Select(client => ToDto(client)).ToListAsync(cancellationToken);

    public async Task<ClientDto> CreateAsync(CreateClientDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (await database.Clients.AnyAsync(client => client.Name == name, cancellationToken))
            throw new ApplicationValidationException("A client with this name already exists.");

        var client = new Client { Name = name, ContactName = TrimOrNull(request.ContactName), ContactEmail = TrimOrNull(request.ContactEmail) };
        database.Clients.Add(client);
        await database.SaveChangesAsync(cancellationToken);
        return ToDto(client);
    }

    public async Task<ClientDto> UpdateAsync(int id, UpdateClientDto request, CancellationToken cancellationToken = default)
    {
        var client = await database.Clients.FindAsync([id], cancellationToken) ?? throw new ApplicationValidationException("Client not found.");
        var name = request.Name.Trim();
        if (await database.Clients.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
            throw new ApplicationValidationException("A client with this name already exists.");

        client.Name = name;
        client.ContactName = TrimOrNull(request.ContactName);
        client.ContactEmail = TrimOrNull(request.ContactEmail);
        client.IsActive = request.IsActive;
        await database.SaveChangesAsync(cancellationToken);
        return ToDto(client);
    }

    private static ClientDto ToDto(Client client) => new(client.Id, client.Name, client.ContactName, client.ContactEmail, client.IsActive);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
