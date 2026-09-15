using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _db;

    public ClientService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ClientDto>> GetAllAsync()
    {
        return await _db.Clients
            .OrderBy(c => c.CompanyName)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                ProjectCount = c.Projects.Count
            })
            .ToListAsync();
    }

    public async Task<ClientDto?> GetByIdAsync(int id)
    {
        return await _db.Clients
            .Where(c => c.Id == id)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                ProjectCount = c.Projects.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<ClientDto>> CreateAsync(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return ServiceResult<ClientDto>.Fail("Company name is required.");

        var client = new Client { CompanyName = companyName.Trim() };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return ServiceResult<ClientDto>.Ok(new ClientDto { Id = client.Id, CompanyName = client.CompanyName, ProjectCount = 0 });
    }

    public async Task<ServiceResult<ClientDto>> UpdateAsync(int id, string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return ServiceResult<ClientDto>.Fail("Company name is required.");

        var client = await _db.Clients.FindAsync(id);
        if (client is null)
            return ServiceResult<ClientDto>.Fail("Client not found.");

        client.CompanyName = companyName.Trim();
        await _db.SaveChangesAsync();

        var projectCount = await _db.Projects.CountAsync(p => p.ClientId == id);
        return ServiceResult<ClientDto>.Ok(new ClientDto { Id = client.Id, CompanyName = client.CompanyName, ProjectCount = projectCount });
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var client = await _db.Clients.Include(c => c.Projects).FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
            return ServiceResult.Fail("Client not found.");

        if (client.Projects.Count > 0)
            return ServiceResult.Fail("Cannot delete a client that still has projects. Remove or reassign its projects first.");

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
