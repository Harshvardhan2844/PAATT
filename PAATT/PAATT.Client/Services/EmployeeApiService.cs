namespace PAATT.Client.Services;
public sealed class EmployeeApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<IReadOnlyList<EmployeeDto>> GetAllAsync() => GetAsync<IReadOnlyList<EmployeeDto>>("api/employees");
    public Task<EmployeeDto> CreateAsync(CreateEmployeeDto request) => SendAsync<EmployeeDto>(HttpMethod.Post, "api/employees", request);
    public Task<EmployeeDto> UpdateRolesAsync(string id, UpdateEmployeeRolesDto request) => SendAsync<EmployeeDto>(HttpMethod.Put, $"api/employees/{id}/roles", request);
}
