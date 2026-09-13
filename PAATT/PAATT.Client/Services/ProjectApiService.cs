namespace PAATT.Client.Services;
public sealed class ProjectApiService(HttpClient client) : ApiClientBase(client)
{
    public Task<IReadOnlyList<ProjectDto>> GetMineAsync() => GetAsync<IReadOnlyList<ProjectDto>>("api/projects/my");
    public Task<IReadOnlyList<ProjectDto>> GetManagedAsync() => GetAsync<IReadOnlyList<ProjectDto>>("api/projects/my-managed");
    public Task<IReadOnlyList<ProjectDto>> GetAllAsync() => GetAsync<IReadOnlyList<ProjectDto>>("api/projects");
    public Task<ProjectDto> CreateAsync(CreateProjectDto request) => SendAsync<ProjectDto>(HttpMethod.Post, "api/projects", request);
}
