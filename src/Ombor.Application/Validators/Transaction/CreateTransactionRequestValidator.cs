using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Application.Validators.Transaction;

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.PartnerId)
            .GreaterThan(0)
            .WithMessage("Invalid Partner ID");

        RuleFor(x => x)
            .MustAsync(async (request, cancellation) =>
            {
                var partner = await context.Partners
                    .Where(p => p.Id == request.PartnerId)
                    .Select(p => new { p.Id, p.Type })
                    .FirstOrDefaultAsync(cancellation);

                if (partner is null)
                {
                    return false;
                }

                return partner.Type.CanHandle(request.Type);
            });

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.TotalPaid)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Payment amount must be positive.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0)
            .WithMessage("Exchange rate must be positive.")
            .Equal(1)
            .When(x => x.Currency == PaymentCurrency.UZS)
            .WithMessage("Exchange rate must be 1 when payment is made in local currency.");

        RuleForEach(r => r.Lines)
            .SetValidator(new CreateTransactionLineValidator());
    }
}
