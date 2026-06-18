using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Domain.Roles;
using Trap_Intel.Infrastructure.Authentication.Configuration;
using Trap_Intel.Infrastructure.Authentication.Services;

namespace Trap_Intel.Tests.Identity;

public class AuthenticationServiceValidateCredentialsTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_WhenUserIsPendingActivation_ReturnsAccountInactive()
    {
        var user = CreateUser("pending.user@example.com", activate: false, confirmEmail: false);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByEmailAsync(user.Email.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHashingService = new Mock<IPasswordHashingService>();

        var service = CreateAuthenticationService(
            userRepository.Object,
            passwordHashingService.Object);

        var result = await service.ValidateCredentialsAsync(user.Email.Value, "StrongPassword!123", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.AccountInactive", result.Errors[0].Code);

        passwordHashingService.Verify(
            service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenUserIsActiveButEmailUnconfirmed_ReturnsEmailNotConfirmed()
    {
        var user = CreateUser("unconfirmed.user@example.com", activate: true, confirmEmail: false);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByEmailAsync(user.Email.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHashingService = new Mock<IPasswordHashingService>();

        var service = CreateAuthenticationService(
            userRepository.Object,
            passwordHashingService.Object);

        var result = await service.ValidateCredentialsAsync(user.Email.Value, "StrongPassword!123", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.EmailNotConfirmed", result.Errors[0].Code);

        passwordHashingService.Verify(
            service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WhenUserIsActiveAndEmailConfirmed_ReturnsSuccess()
    {
        var user = CreateUser("confirmed.user@example.com", activate: false, confirmEmail: true);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(repository => repository.GetByEmailAsync(user.Email.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHashingService = new Mock<IPasswordHashingService>();
        passwordHashingService
            .Setup(service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Success);

        var service = CreateAuthenticationService(
            userRepository.Object,
            passwordHashingService.Object);

        var result = await service.ValidateCredentialsAsync(user.Email.Value, "StrongPassword!123", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);

        passwordHashingService.Verify(
            service => service.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    private static AuthenticationService CreateAuthenticationService(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService)
    {
        var jwtTokenService = new Mock<IJwtTokenService>();
        var refreshTokenService = new Mock<IRefreshTokenService>();
        var roleRepository = new Mock<IRoleRepository>();

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "ThisIsATestOnlySecretKeyWithAtLeast32Chars!",
            Issuer = "trap-intel-tests",
            Audience = "trap-intel-tests",
            AccessTokenExpirationMinutes = 15
        });

        var lockoutSettings = Options.Create(new LockoutSettings
        {
            EnableLockout = true,
            MaxFailedAttempts = 5,
            LockoutDurationMinutes = 15
        });

        return new AuthenticationService(
            userRepository,
            passwordHashingService,
            jwtTokenService.Object,
            refreshTokenService.Object,
            roleRepository.Object,
            jwtSettings,
            lockoutSettings,
            NullLogger<AuthenticationService>.Instance);
    }

    private static User CreateUser(string email, bool activate, bool confirmEmail)
    {
        var emailResult = UserEmail.Create(email);
        Assert.True(emailResult.IsSuccess);

        var userNameResult = UserName.Create(email);
        Assert.True(userNameResult.IsSuccess);

        var firstNameResult = FirstName.Create("Test");
        Assert.True(firstNameResult.IsSuccess);

        var lastNameResult = LastName.Create("User");
        Assert.True(lastNameResult.IsSuccess);

        var userResult = User.Create(
            Guid.NewGuid(),
            emailResult.Value,
            userNameResult.Value,
            firstNameResult.Value,
            lastNameResult.Value,
            SystemRoles.ViewerId);

        Assert.True(userResult.IsSuccess);

        var user = userResult.Value;

        if (confirmEmail)
        {
            var confirmResult = user.ConfirmEmail();
            Assert.True(confirmResult.IsSuccess);
        }
        else if (activate)
        {
            var activateResult = user.Activate();
            Assert.True(activateResult.IsSuccess);
        }

        return user;
    }
}
