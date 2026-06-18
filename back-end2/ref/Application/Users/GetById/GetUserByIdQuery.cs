using Application.Abstractions.Messaging;
using Application.Users.Common;

namespace Application.Users.GetById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
