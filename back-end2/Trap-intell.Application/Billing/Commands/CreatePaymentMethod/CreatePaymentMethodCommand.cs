using FluentValidation;
using MediatR;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Application.Billing.Commands.CreatePaymentMethod;

public sealed record CreatePaymentMethodCommand(
    Guid OrganizationId,
    PaymentMethodType Type,
    string? LastFourDigits,
    string? CardBrand,
    string? PaymentProcessor,
    string? Token,
    DateTime? ExpiresAt,
    string? BillingContactEmail,
    bool IsDefault) : IRequest<Result<Guid>>;

public sealed class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.BillingContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.BillingContactEmail));

        RuleFor(x => x.LastFourDigits)
            .Matches("^[0-9]{4}$")
            .When(x => !string.IsNullOrWhiteSpace(x.LastFourDigits))
            .WithMessage("Last four digits must be exactly 4 digits.");
    }
}
