using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using Trap_Intel.Application.Users.Queries.GetUsers; // Reusing UserDto

namespace Trap_Intel.Application.Users.Queries.GetUserById;

internal sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(IdentityErrors.UserNotFound_Detail(request.UserId.ToString()));
        }

        var dto = new UserDto(
            user.Id,
            user.Email.Value,
            user.UserName.Value,
            user.FirstName.Value,
            user.LastName.Value,
            $"{user.FirstName.Value} {user.LastName.Value}",
            user.Status,
            user.RoleId,
            user.OrganizationId,
            user.CreatedAt
        );

        return Result.Success(dto);
    }
}
