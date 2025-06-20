using FluentValidation;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Application.Validators.Transaction;

public sealed class DeleteTransactionRequestValidator : AbstractValidator<DeleteTransactionRequest>
{
    public DeleteTransactionRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Transaction ID.");
    }
}
