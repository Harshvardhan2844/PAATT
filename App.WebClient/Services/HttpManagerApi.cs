using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;
using App.Shared.Enums;

namespace App.WebClient.Services;

/// <summary>
/// WebAssembly-side implementation of IManagerApi. Every route re-resolves the
/// signed-in user and re-checks the Manager assignment server-side; nothing
/// here is trusted.
/// </summary>
public class HttpManagerApi : IManagerApi
{
    private const string BaseRoute = "api/manager";
    private const string OfflineMessage = "Could not reach the server. Check your connection and try again.";

    private readonly HttpClient _http;

    public HttpManagerApi(HttpClient http)
    {
        _http = http;
    }

    public Task<List<ProjectDto>> GetMyProjectsAsync()
        => GetListAsync<ProjectDto>($"{BaseRoute}/projects");

    public Task<ProjectDto?> GetProjectAsync(int projectId)
        => GetOrNullAsync<ProjectDto>($"{BaseRoute}/projects/{projectId}");

    public Task<List<BudgetVsActualDto>> GetProjectBudgetsAsync(int projectId)
        => GetListAsync<BudgetVsActualDto>($"{BaseRoute}/projects/{projectId}/budgets");

    public Task<List<TimesheetDto>> GetQueueAsync(TimesheetStatus? status = null)
        => GetListAsync<TimesheetDto>(status is null
            ? $"{BaseRoute}/timesheets"
            : $"{BaseRoute}/timesheets?status={status}");

    public Task<TimesheetDto?> GetTimesheetAsync(int timesheetId)
        => GetOrNullAsync<TimesheetDto>($"{BaseRoute}/timesheets/{timesheetId}");

    public Task<List<TimesheetReviewEventDto>> GetHistoryAsync(int timesheetId)
        => GetListAsync<TimesheetReviewEventDto>($"{BaseRoute}/timesheets/{timesheetId}/history");

    public Task<ServiceResult> ApproveTimesheetAsync(int timesheetId)
        => PostAsync($"{BaseRoute}/timesheets/{timesheetId}/approve", new { });

    public Task<ServiceResult> RejectTimesheetAsync(int timesheetId, RejectionRequest request)
        => PostAsync($"{BaseRoute}/timesheets/{timesheetId}/reject", request);

    public Task<ServiceResult> RejectEntryAsync(int entryId, RejectionRequest request)
        => PostAsync($"{BaseRoute}/entries/{entryId}/reject", request);

    // ---------- Plumbing ----------

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<T>>(url) ?? new List<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new List<T>();
        }
    }

    private async Task<T?> GetOrNullAsync<T>(string url) where T : class
    {
        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
                return null;

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<ServiceResult> PostAsync<TBody>(string url, TBody body)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(url, body);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
                if (result is not null) return result;
            }

            return ServiceResult.Fail(DescribeFailure(response));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult.Fail(OfflineMessage);
        }
    }

    private static string DescribeFailure(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => "Your session has expired. Sign in again.",
        HttpStatusCode.Forbidden => "You don't have permission to do that.",
        HttpStatusCode.NotFound => "That item no longer exists. Refresh the page.",
        _ => $"The server rejected the request ({(int)response.StatusCode})."
    };
}
