using PAATT.Shared.DTOs;

namespace PAATT.Client.Services;

public sealed class AccountApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<CurrentUserDto> GetCurrentUserAsync() => GetAsync<CurrentUserDto>("api/account/me");
    public Task LoginAsync(LoginDto request) => SendAsync(HttpMethod.Post, "api/account/login", request);
    public Task LogoutAsync() => SendAsync(HttpMethod.Post, "api/account/logout");
}
