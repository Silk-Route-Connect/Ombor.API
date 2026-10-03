using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;

namespace Ombor.Application.Validators.Payment;

public sealed class CreatePaymentRecordRequestValidator : AbstractValidator<CreatePaymentRecordRequest>
{
    public CreatePaymentRecordRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid payment type.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Invalid payment direction.");

        RuleFor(x => x.WalletId)
            .GreaterThan(0)
            .WithMessage("A wallet is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.PartnerId)
            .NotNull()
            .When(x => x.Type is PaymentType.Transaction or PaymentType.Deposit or PaymentType.Withdrawal)
            .WithMessage("A partner is required for this payment type.");

        RuleFor(x => x.EmployeeId)
            .NotNull()
            .When(x => x.Type == PaymentType.Payroll)
            .WithMessage("An employee is required for a payroll payment.");

        // Rule 14: payroll is always money out.
        RuleFor(x => x.Direction)
            .Equal(PaymentDirection.Expense)
            .When(x => x.Type == PaymentType.Payroll)
            .WithMessage("A payroll payment is always an expense.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .When(x => x.Type == PaymentType.General)
            .WithMessage("A description is required for a general payment.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Description must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Period)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Period must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Attachments)
            .Must(files => files is null || files.Length <= ValidationConstants.MaxAttachments)
            .WithMessage($"At most {ValidationConstants.MaxAttachments} files can be attached.");

        RuleForEach(x => x.Settlements).ChildRules(settlement =>
        {
            settlement.RuleFor(s => s.TransactionId).GreaterThan(0).WithMessage("Invalid transaction.");
            settlement.RuleFor(s => s.Amount).GreaterThan(0).WithMessage("Settlement amount must be greater than zero.");
        });
    }
}
