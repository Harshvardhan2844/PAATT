namespace PAATT.Client.Services;
public sealed class ClientApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<IReadOnlyList<ClientDto>> GetAllAsync() => GetAsync<IReadOnlyList<ClientDto>>("api/clients");
    public Task<ClientDto> CreateAsync(CreateClientDto request) => SendAsync<ClientDto>(HttpMethod.Post, "api/clients", request);
}
