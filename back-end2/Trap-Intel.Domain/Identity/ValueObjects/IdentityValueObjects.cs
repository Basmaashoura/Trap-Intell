using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Value objects for the Identity domain.
    /// Immutable, self-validating domain concepts.
    /// </summary>

    /// <summary>
    /// Represents a user email address with validation.
    /// </summary>
    public record UserEmail
    {
        public string Value { get; }

        private UserEmail(string value) => Value = value;

        public static Result<UserEmail> Create(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<UserEmail>(IdentityErrors.InvalidUserEmail);

            var trimmed = email.Trim().ToLowerInvariant();

            if (!trimmed.Contains("@") || !trimmed.Contains("."))
                return Result.Failure<UserEmail>(IdentityErrors.InvalidUserEmail);

            if (trimmed.Length > 254)
                return Result.Failure<UserEmail>(IdentityErrors.InvalidUserEmail);

            return Result.Success(new UserEmail(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a user name with validation.
    /// </summary>
    public record UserName
    {
        public string Value { get; }

        private UserName(string value) => Value = value;

        public static Result<UserName> Create(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<UserName>(IdentityErrors.InvalidUserName);

            var trimmed = name.Trim();

            if (trimmed.Length < 3 || trimmed.Length > 50)
                return Result.Failure<UserName>(IdentityErrors.InvalidUserName);

            return Result.Success(new UserName(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a person's first name.
    /// </summary>
    public record FirstName
    {
        public string Value { get; }

        private FirstName(string value) => Value = value;

        public static Result<FirstName> Create(string? firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<FirstName>(IdentityErrors.InvalidUserFirstName);

            var trimmed = firstName.Trim();

            if (trimmed.Length < 2 || trimmed.Length > 100)
                return Result.Failure<FirstName>(IdentityErrors.InvalidUserFirstName);

            return Result.Success(new FirstName(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a person's last name.
    /// </summary>
    public record LastName
    {
        public string Value { get; }

        private LastName(string value) => Value = value;

        public static Result<LastName> Create(string? lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<LastName>(IdentityErrors.InvalidUserLastName);

            var trimmed = lastName.Trim();

            if (trimmed.Length < 2 || trimmed.Length > 100)
                return Result.Failure<LastName>(IdentityErrors.InvalidUserLastName);

            return Result.Success(new LastName(trimmed));
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents user preferences for notifications and UI settings.
    /// </summary>
    public record UserPreferences
    {
        public string Language { get; init; } = "en";
        public string Timezone { get; init; } = "UTC";
        public bool EmailNotificationsEnabled { get; init; } = true;
        public bool PushNotificationsEnabled { get; init; } = true;
        public bool DarkModeEnabled { get; init; }
        public int SessionTimeoutMinutes { get; init; } = 30;

        // Private parameterless constructor for EF Core
        private UserPreferences() { }

        private UserPreferences(
            string language,
            string timezone,
            bool emailNotificationsEnabled,
            bool pushNotificationsEnabled,
            bool darkModeEnabled,
            int sessionTimeoutMinutes)
        {
            Language = language;
            Timezone = timezone;
            EmailNotificationsEnabled = emailNotificationsEnabled;
            PushNotificationsEnabled = pushNotificationsEnabled;
            DarkModeEnabled = darkModeEnabled;
            SessionTimeoutMinutes = sessionTimeoutMinutes;
        }

        public static Result<UserPreferences> Create(
            string language = "en",
            string timezone = "UTC",
            bool emailNotifications = true,
            bool pushNotifications = true,
            bool darkMode = false,
            int sessionTimeout = 30)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Result.Failure<UserPreferences>(IdentityErrors.InvalidUserPreferences);

            if (string.IsNullOrWhiteSpace(timezone))
                return Result.Failure<UserPreferences>(IdentityErrors.InvalidUserPreferences);

            if (sessionTimeout <= 0 || sessionTimeout > 1440)
                return Result.Failure<UserPreferences>(IdentityErrors.InvalidUserPreferences);

            return Result.Success(new UserPreferences(
                language,
                timezone,
                emailNotifications,
                pushNotifications,
                darkMode,
                sessionTimeout));
        }

        public static UserPreferences Default() =>
            new("en", "UTC", true, true, false, 30);
    }
}
