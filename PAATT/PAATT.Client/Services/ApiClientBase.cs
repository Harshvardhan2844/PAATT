using System.Net.Http.Json;

namespace PAATT.Client.Services;

public abstract class ApiClientBase(HttpClient httpClient)
{
    protected async Task<T> GetAsync<T>(string url, CancellationToken token = default) => await httpClient.GetFromJsonAsync<T>(url, token) ?? throw new InvalidOperationException("The server returned an empty response.");
    protected async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await httpClient.SendAsync(request, token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ReadErrorAsync(response));
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token) ?? throw new InvalidOperationException("The server returned an empty response.");
    }
    protected async Task SendAsync(HttpMethod method, string url, object? body = null, CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        using var response = await httpClient.SendAsync(request, token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(await ReadErrorAsync(response));
    }
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response) => $"Request failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}";
}
