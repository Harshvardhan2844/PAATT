using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;

namespace App.WebClient.Services;

/// <summary>
/// WebAssembly-side implementation of IConsultantApi. Everything goes over
/// HTTP to /api/consultant/*, which re-resolves the signed-in user and
/// re-checks ownership server-side. Failures are folded into ServiceResult so
/// a dropped connection reads as a message in the page rather than an
/// unhandled exception.
/// </summary>
public class HttpConsultantApi : IConsultantApi
{
    private const string BaseRoute = "api/consultant";

    private readonly HttpClient _http;

    public HttpConsultantApi(HttpClient http)
    {
        _http = http;
    }

    public Task<List<ConsultantProjectSummaryDto>> GetMyProjectsAsync()
        => GetListAsync<ConsultantProjectSummaryDto>($"{BaseRoute}/projects");

    public Task<List<TimesheetDto>> GetMyTimesheetsAsync(int? projectId = null)
        => GetListAsync<TimesheetDto>(projectId is null
            ? $"{BaseRoute}/timesheets"
            : $"{BaseRoute}/timesheets?projectId={projectId.Value}");

    public async Task<TimesheetDto?> GetTimesheetForWeekAsync(int projectId, DateOnly weekStarting)
    {
        var url = $"{BaseRoute}/timesheets/week?projectId={projectId}&weekStarting={TimeRules.ToRouteValue(weekStarting)}";

        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
                return null;

            return await response.Content.ReadFromJsonAsync<TimesheetDto>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    public Task<List<DailyTotalDto>> GetDailyTotalsAsync(DateOnly weekStarting)
        => GetListAsync<DailyTotalDto>($"{BaseRoute}/daily-totals?weekStarting={TimeRules.ToRouteValue(weekStarting)}");

    public async Task<ServiceResult<TimesheetEntryDto>> AddEntryAsync(AddEntryRequest request)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync($"{BaseRoute}/entries", request);
            return await ReadResultAsync<TimesheetEntryDto>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult<TimesheetEntryDto>.Fail(OfflineMessage);
        }
    }

    public async Task<ServiceResult<TimesheetEntryDto>> UpdateEntryAsync(int entryId, UpdateEntryRequest request)
    {
        try
        {
            using var response = await _http.PutAsJsonAsync($"{BaseRoute}/entries/{entryId}", request);
            return await ReadResultAsync<TimesheetEntryDto>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult<TimesheetEntryDto>.Fail(OfflineMessage);
        }
    }

    public async Task<ServiceResult> DeleteEntryAsync(int entryId)
    {
        try
        {
            using var response = await _http.DeleteAsync($"{BaseRoute}/entries/{entryId}");
            return await ReadResultAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult.Fail(OfflineMessage);
        }
    }

    public async Task<ServiceResult> SubmitTimesheetAsync(int timesheetId)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync($"{BaseRoute}/timesheets/{timesheetId}/submit", new { });
            return await ReadResultAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult.Fail(OfflineMessage);
        }
    }

    // ---------- Plumbing ----------

    private const string OfflineMessage = "Could not reach the server. Check your connection and try again.";

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<T>>(url);
            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new List<T>();
        }
    }

    private static async Task<ServiceResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ServiceResult<T>>();
            if (body is not null) return body;
        }

        return ServiceResult<T>.Fail(DescribeFailure(response));
    }

    private static async Task<ServiceResult> ReadResultAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (body is not null) return body;
        }

        return ServiceResult.Fail(DescribeFailure(response));
    }

    private static string DescribeFailure(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => "Your session has expired. Sign in again.",
        HttpStatusCode.Forbidden => "You don't have permission to do that.",
        HttpStatusCode.NotFound => "That item no longer exists. Refresh the page.",
        _ => $"The server rejected the request ({(int)response.StatusCode})."
    };
}
