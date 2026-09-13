using PAATT.Shared.DTOs;

namespace PAATT.Services.Interfaces;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateRolesAsync(string userId, UpdateEmployeeRolesDto request, CancellationToken cancellationToken = default);
}
