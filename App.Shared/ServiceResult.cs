namespace App.Shared;

/// <summary>
/// Wraps a service-layer outcome so callers (Blazor Server components or the
/// API endpoints App.WebClient calls) get back validation failures — e.g.
/// "24-hour cap exceeded", "entry is locked", "already a Manager on this
/// project" — instead of exceptions, and can show them directly in the UI.
/// </summary>
public class ServiceResult
{
    public bool Success { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, Errors = { error } };
    public static ServiceResult Fail(IEnumerable<string> errors) => new() { Success = false, Errors = errors.ToList() };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Success = false, Errors = { error } };
    public new static ServiceResult<T> Fail(IEnumerable<string> errors) => new() { Success = false, Errors = errors.ToList() };
}
