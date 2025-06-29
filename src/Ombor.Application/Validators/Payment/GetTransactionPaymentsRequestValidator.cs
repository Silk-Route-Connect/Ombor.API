using FluentValidation;
using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Validators.Payment;

public sealed class GetTransactionPaymentsRequestValidator : AbstractValidator<GetTransactionPaymentsRequest>
{
    public GetTransactionPaymentsRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Transaction ID must be greater than 0.");
    }
}
