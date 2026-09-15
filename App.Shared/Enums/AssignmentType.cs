namespace App.Shared.Enums;

/// <summary>
/// Determines whether an employee's ProjectAssignment row makes them the
/// Manager or a Consultant on that specific project. An employee can be
/// a Manager on one project and a Consultant on another (never both on
/// the same project — enforced in the service layer).
/// </summary>
public enum AssignmentType
{
    Manager = 0,
    Consultant = 1
}
