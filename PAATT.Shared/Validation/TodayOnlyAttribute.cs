using System.ComponentModel.DataAnnotations;

namespace PAATT.Shared.Validation;

public sealed class TodayOnlyAttribute : ValidationAttribute
{
    public TodayOnlyAttribute() : base("Entries can only be created or changed for today.") { }
    public override bool IsValid(object? value) => value is DateOnly date && date == AppClock.Today;
}
