using PAATT.Shared.DTOs;

namespace PAATT.Client.Services;

public sealed class AccountApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<CurrentUserDto> GetCurrentUserAsync() => GetAsync<CurrentUserDto>("api/account/me");
}
