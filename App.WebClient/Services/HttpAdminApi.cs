using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;

namespace App.WebClient.Services;

/// <summary>
/// WebAssembly-side implementation of IAdminApi: everything goes over HTTP to
/// the /api/admin endpoints in App.Web, which re-check the Admin role
/// server-side. Cookies ride along automatically for same-origin requests, so
/// there is no token handling here.
///
/// Failures are folded into ServiceResult rather than thrown, so a dropped
/// connection or an expired session shows up as a message in the page instead
/// of an unhandled exception in the browser console.
/// </summary>
public class HttpAdminApi : IAdminApi
{
    private const string BaseRoute = "api/admin";

    private readonly HttpClient _http;

    public HttpAdminApi(HttpClient http)
    {
        _http = http;
    }

    // ---------- Employees ----------

    public Task<List<EmployeeDto>> GetEmployeesAsync()
        => GetListAsync<EmployeeDto>($"{BaseRoute}/employees");

    public Task<List<EmployeeDto>> GetManagersAsync()
        => GetListAsync<EmployeeDto>($"{BaseRoute}/employees/managers");

    public Task<List<EmployeeDto>> GetConsultantsAsync()
        => GetListAsync<EmployeeDto>($"{BaseRoute}/employees/consultants");

    public Task<ServiceResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request)
        => PostAsync<CreateEmployeeRequest, EmployeeDto>($"{BaseRoute}/employees", request);

    public Task<ServiceResult<EmployeeDto>> UpdateEmployeeAsync(string employeeId, UpdateEmployeeRequest request)
        => PutAsync<UpdateEmployeeRequest, EmployeeDto>($"{BaseRoute}/employees/{employeeId}", request);

    public Task<ServiceResult> SetAdminAsync(string employeeId, bool isAdmin)
        => PostAsync($"{BaseRoute}/employees/{employeeId}/admin", new SetAdminRequest { IsAdmin = isAdmin });

    public Task<ServiceResult> DeleteEmployeeAsync(string employeeId)
        => DeleteAsync($"{BaseRoute}/employees/{employeeId}");

    // ---------- Clients ----------

    public Task<List<ClientDto>> GetClientsAsync()
        => GetListAsync<ClientDto>($"{BaseRoute}/clients");

    public Task<ServiceResult<ClientDto>> CreateClientAsync(ClientRequest request)
        => PostAsync<ClientRequest, ClientDto>($"{BaseRoute}/clients", request);

    public Task<ServiceResult<ClientDto>> UpdateClientAsync(int clientId, ClientRequest request)
        => PutAsync<ClientRequest, ClientDto>($"{BaseRoute}/clients/{clientId}", request);

    public Task<ServiceResult> DeleteClientAsync(int clientId)
        => DeleteAsync($"{BaseRoute}/clients/{clientId}");

    // ---------- Projects ----------

    public Task<List<ProjectDto>> GetProjectsAsync()
        => GetListAsync<ProjectDto>($"{BaseRoute}/projects");

    public async Task<ProjectDto?> GetProjectAsync(int projectId)
    {
        try
        {
            using var response = await _http.GetAsync($"{BaseRoute}/projects/{projectId}");
            if (response.StatusCode == HttpStatusCode.NotFound || !response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ProjectDto>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    public Task<ServiceResult<ProjectDto>> CreateProjectAsync(ProjectRequest request)
        => PostAsync<ProjectRequest, ProjectDto>($"{BaseRoute}/projects", request);

    public Task<ServiceResult<ProjectDto>> UpdateProjectAsync(int projectId, ProjectRequest request)
        => PutAsync<ProjectRequest, ProjectDto>($"{BaseRoute}/projects/{projectId}", request);

    public Task<ServiceResult> DeleteProjectAsync(int projectId)
        => DeleteAsync($"{BaseRoute}/projects/{projectId}");

    // ---------- Assignments ----------

    public Task<List<ProjectAssignmentDto>> GetAssignmentsForProjectAsync(int projectId)
        => GetListAsync<ProjectAssignmentDto>($"{BaseRoute}/projects/{projectId}/assignments");

    public Task<ServiceResult<ProjectAssignmentDto>> AssignAsync(AssignmentRequest request)
        => PostAsync<AssignmentRequest, ProjectAssignmentDto>($"{BaseRoute}/assignments", request);

    public Task<ServiceResult> RemoveAssignmentAsync(int assignmentId)
        => DeleteAsync($"{BaseRoute}/assignments/{assignmentId}");

    // ---------- Plumbing ----------

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<T>>(url);
            return result ?? new List<T>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            // A read failing shouldn't blow up the page — the list just comes
            // back empty and the page renders its "nothing here" state.
            return new List<T>();
        }
    }

    private async Task<ServiceResult<TResult>> PostAsync<TRequest, TResult>(string url, TRequest request)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(url, request);
            return await ReadResultAsync<TResult>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult<TResult>.Fail("Could not reach the server. Check your connection and try again.");
        }
    }

    private async Task<ServiceResult> PostAsync<TRequest>(string url, TRequest request)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(url, request);
            return await ReadResultAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult.Fail("Could not reach the server. Check your connection and try again.");
        }
    }

    private async Task<ServiceResult<TResult>> PutAsync<TRequest, TResult>(string url, TRequest request)
    {
        try
        {
            using var response = await _http.PutAsJsonAsync(url, request);
            return await ReadResultAsync<TResult>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult<TResult>.Fail("Could not reach the server. Check your connection and try again.");
        }
    }

    private async Task<ServiceResult> DeleteAsync(string url)
    {
        try
        {
            using var response = await _http.DeleteAsync(url);
            return await ReadResultAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ServiceResult.Fail("Could not reach the server. Check your connection and try again.");
        }
    }

    /// <summary>
    /// Business-rule failures come back as HTTP 200 with Success = false, so
    /// the body is the authority. A non-200 means something outside the
    /// business rules went wrong (session expired, not an Admin, server error).
    /// </summary>
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
