using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetCurrentUser;

/// <summary>
/// Query to get the currently authenticated user's information.
/// </summary>
public sealed record GetCurrentUserQuery : IQuery<UserResponse>;
