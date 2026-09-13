using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace PAATT.Shared.Validation;

public sealed class ValidEmailAttribute : ValidationAttribute
{
    public ValidEmailAttribute() : base("Enter a valid email address.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email)) return true;
        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address == email.Trim() && parsed.Host.Contains('.', StringComparison.Ordinal);
        }
        catch (FormatException) { return false; }
    }
}
