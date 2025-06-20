using FluentValidation;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Application.Validators.Transaction;

public sealed class GetTransactionByIdRequestValidator : AbstractValidator<GetTransactionByIdRequest>
{
    public GetTransactionByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Transaction ID.");
    }
}
