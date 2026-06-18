using MediatR;
using Trap_Intel.Domain.Abstractions;
using System;

namespace Trap_Intel.Application.Users.Commands.UpdateCurrentUserProfile;

public sealed record UpdateCurrentUserProfileCommand(
    Guid UserId,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    string? JobTitle = null,
    string? Department = null,
    string? Location = null,
    string? Bio = null,
    string? WebsiteUrl = null,
    string? LinkedInUrl = null,
    string? GitHubUrl = null,
    string? XUrl = null
) : IRequest<Result>;
