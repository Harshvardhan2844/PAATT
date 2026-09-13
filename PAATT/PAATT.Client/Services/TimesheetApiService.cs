namespace PAATT.Client.Services;
public sealed class TimesheetApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<IReadOnlyList<TimesheetDto>> GetMineAsync() => GetAsync<IReadOnlyList<TimesheetDto>>("api/timesheets/my");
    public Task<IReadOnlyList<TimesheetDto>> GetReviewAsync(TimesheetStatus status) => GetAsync<IReadOnlyList<TimesheetDto>>($"api/timesheets/review/{status}");
    public Task<TimesheetDetailsDto> GetDetailsAsync(int id) => GetAsync<TimesheetDetailsDto>($"api/timesheets/{id}");
    public Task<TimesheetDto> CreateAsync(CreateTimesheetDto request) => SendAsync<TimesheetDto>(HttpMethod.Post, "api/timesheets", request);
    public Task<TimesheetDetailsDto> AddEntryAsync(int id, CreateTimesheetEntryDto request) => SendAsync<TimesheetDetailsDto>(HttpMethod.Post, $"api/timesheets/{id}/entries", request);
    public Task SubmitAsync(int id) => SendAsync(HttpMethod.Post, $"api/timesheets/{id}/submit");
    public Task ApproveAsync(int id) => SendAsync(HttpMethod.Post, $"api/timesheets/{id}/approve");
    public Task RejectAsync(int id, RejectTimesheetDto request) => SendAsync(HttpMethod.Post, $"api/timesheets/{id}/reject", request);
    public Task RejectEntryAsync(int id, RejectTimesheetEntryDto request) => SendAsync(HttpMethod.Post, $"api/timesheet-entries/{id}/reject", request);
}
