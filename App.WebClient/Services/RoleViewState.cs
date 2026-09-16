namespace App.WebClient.Services;

public enum RoleView
{
    Admin,
    Manager,
    Consultant
}

/// <summary>Shared selected workspace view for the sidebar and top navigation.</summary>
public sealed class RoleViewState
{
    public RoleView? ActiveView { get; private set; }

    public event Action? Changed;

    public void Select(RoleView view)
    {
        if (ActiveView == view) return;
        ActiveView = view;
        Changed?.Invoke();
    }
}
