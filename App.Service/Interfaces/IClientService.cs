using App.Shared;
using App.Shared.Dtos;

namespace App.Service.Interfaces;

public interface IClientService
{
    Task<List<ClientDto>> GetAllAsync();

    Task<ClientDto?> GetByIdAsync(int id);

    Task<ServiceResult<ClientDto>> CreateAsync(string companyName);

    Task<ServiceResult<ClientDto>> UpdateAsync(int id, string companyName);

    /// <summary>Fails if the client has any projects — remove/reassign those first.</summary>
    Task<ServiceResult> DeleteAsync(int id);
}
