namespace App.Shared.Dtos.Requests;

/// <summary>
/// Grants (true) or revokes (false) the Admin Identity role on an existing
/// user. Admin stacks on top of Consultant — the Consultant role is never
/// removed, so a promoted Admin can still be assigned to projects.
/// </summary>
public class SetAdminRequest
{
    public bool IsAdmin { get; set; }
}
