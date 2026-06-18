using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Trap_Intel.Application.Abstractions.Identity;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;

namespace Trap_Intel.Infrastructure.Authentication.Identity;

/// <summary>
/// Adapts ASP.NET Core Identity's UserManager and SignInManager capabilities 
/// to our application-specific IIdentityService port.
/// Returns pure Domain.Identity.User models.
/// </summary>
internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository; // The DDD repository pointing to the pure Entity
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<User>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser == null)
        {
            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        var isValid = await _userManager.CheckPasswordAsync(appUser, password);
        if (!isValid)
        {
            // Identity tracks failed access events if configured through options
            await _userManager.AccessFailedAsync(appUser);

            if (await _userManager.IsLockedOutAsync(appUser))
            {
                return Result.Failure<User>(IdentityErrors.AccountLocked);
            }

            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        // Successfully authenticated, clear lockout
        await _userManager.ResetAccessFailedCountAsync(appUser);

        // Fetch our Domain Aggregate root representation
        var domainUser = await _userRepository.GetByIdAsync(appUser.Id, cancellationToken);
        if (domainUser == null)
        {
            _logger.LogError("Application user {Id} exists, but Domain User aggregate was not found.", appUser.Id);
            return Result.Failure<User>(IdentityErrors.InvalidCredentials); // Or Server Error
        }

        if (!domainUser.IsActive)
        {
            return Result.Failure<User>(IdentityErrors.AccountInactive);
        }

        if (!domainUser.EmailConfirmed)
        {
            return Result.Failure<User>(IdentityErrors.EmailNotConfirmed);
        }

        // Raise Login Success inside Domain if we choose to model it there
        domainUser.RecordSuccessfulLogin();

        return Result.Success(domainUser);
    }

    public async Task<Result> RegisterUserAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var appUser = new ApplicationUser
        {
            Id = user.Id, // Link IDs
            UserName = user.UserName.Value,
            Email = user.Email.Value,
            FirstName = user.FirstName.Value,
            LastName = user.LastName.Value,
            OrganizationId = user.OrganizationId
        };

        var result = await _userManager.CreateAsync(appUser, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).FirstOrDefault() ?? "Registration failed";
            return Result.Failure(Error.Custom("Identity.RegistrationFailed", errors));
        }

        if (string.IsNullOrWhiteSpace(appUser.PasswordHash))
        {
            await _userManager.DeleteAsync(appUser);
            return Result.Failure(Error.Custom("Identity.RegistrationFailed", "Identity did not generate a password hash."));
        }

        var setPasswordHashResult = user.SetPasswordHash(appUser.PasswordHash);
        if (setPasswordHashResult.IsFailure)
        {
            await _userManager.DeleteAsync(appUser);
            return setPasswordHashResult;
        }

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
        if (appUser == null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        var result = await _userManager.ChangePasswordAsync(appUser, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).FirstOrDefault() ?? "Password change failed";
            return Result.Failure(Error.Custom("Identity.PasswordChangeFailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser == null)
        {
            return Result.Failure<string>(IdentityErrors.UserNotFound);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(appUser);
        return Result.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser == null)
        {
            return Result.Failure(IdentityErrors.UserNotFound);
        }

        var result = await _userManager.ResetPasswordAsync(appUser, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).FirstOrDefault() ?? "Password reset failed";
            return Result.Failure(Error.Custom("Identity.PasswordResetFailed", errors));
        }

        return Result.Success();
    }
}