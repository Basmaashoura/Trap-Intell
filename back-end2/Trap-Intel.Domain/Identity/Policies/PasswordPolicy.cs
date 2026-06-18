using System.Text.RegularExpressions;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Identity.Policies;

/// <summary>
/// Password complexity and validation policy.
/// OWASP compliant password requirements.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;
    public const int RequiredUniqueCharacters = 4;

    /// <summary>
    /// Validate password complexity.
    /// </summary>
    public static Result ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(IdentityErrors.InvalidPassword);
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long.");
        }

        if (password.Length > MaximumLength)
        {
            errors.Add($"Password cannot exceed {MaximumLength} characters.");
        }

        if (!HasUpperCase(password))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!HasLowerCase(password))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!HasDigit(password))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!HasSpecialCharacter(password))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (password.Distinct().Count() < RequiredUniqueCharacters)
        {
            errors.Add($"Password must contain at least {RequiredUniqueCharacters} unique characters.");
        }

        if (HasCommonPattern(password))
        {
            errors.Add("Password contains a common pattern and is easily guessable.");
        }

        if (errors.Count > 0)
        {
            return Result.Failure(
                Error.Custom("Identity.PasswordTooWeak", string.Join(" ", errors)));
        }

        return Result.Success();
    }

    /// <summary>
    /// Check if password is in common password list.
    /// </summary>
    public static bool IsCommonPassword(string password)
    {
        // Top 100 most common passwords (abbreviated list)
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123",
            "monkey", "1234567", "letmein", "trustno1", "dragon",
            "baseball", "iloveyou", "master", "sunshine", "ashley",
            "bailey", "passw0rd", "shadow", "123123", "654321",
            "superman", "qazwsx", "michael", "football", "password1",
            "password123", "welcome", "welcome1", "admin", "login"
        };

        return commonPasswords.Contains(password);
    }

    private static bool HasUpperCase(string password) =>
        password.Any(char.IsUpper);

    private static bool HasLowerCase(string password) =>
        password.Any(char.IsLower);

    private static bool HasDigit(string password) =>
        password.Any(char.IsDigit);

    private static bool HasSpecialCharacter(string password) =>
        password.Any(c => !char.IsLetterOrDigit(c));

    private static bool HasCommonPattern(string password)
    {
        var patterns = new[]
        {
            @"(.)\1{2,}",           // Repeated characters (aaa, 111)
            @"(012|123|234|345|456|567|678|789|890)", // Sequential numbers
            @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", // Sequential letters
            @"(qwerty|asdf|zxcv)"   // Keyboard patterns
        };

        return patterns.Any(pattern =>
            Regex.IsMatch(password, pattern, RegexOptions.IgnoreCase));
    }
}
