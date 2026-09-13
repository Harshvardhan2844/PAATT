using System.ComponentModel.DataAnnotations;

namespace PAATT.Shared.Validation;

public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute() : base("Password must have at least 8 characters, with upper-case, lower-case, a number, and a symbol.") { }
    public override bool IsValid(object? value)
    {
        if (value is not string password) return false;
        return password.Length >= 8 && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit) && password.Any(character => !char.IsLetterOrDigit(character));
    }
}
