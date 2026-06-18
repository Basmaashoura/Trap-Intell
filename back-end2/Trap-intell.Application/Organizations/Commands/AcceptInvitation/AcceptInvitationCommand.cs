using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Application.Organizations.Commands.AcceptInvitation;

public sealed record AcceptInvitationCommand(
	string RawToken,
	Guid AcceptingUserId) : IRequest<Result>;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
	public AcceptInvitationCommandValidator()
	{
		RuleFor(x => x.RawToken).NotEmpty();
		RuleFor(x => x.AcceptingUserId).NotEmpty();
	}
}
