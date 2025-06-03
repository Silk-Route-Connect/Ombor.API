using System;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Contracts.Requests.Supplier;

namespace Ombor.Application.Validators.Supplier;

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid supplier ID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Description must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Email)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Email must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Company name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleForEach(x => x.PhoneNumbers)
            .Must(IsValidPhoneNumber)
            .WithMessage("Telefon raqami noto'g'ri formatda");

    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        return Regex.IsMatch(phoneNumber, @"^\+998(9[0-9]|8[8])\d{7}$");
    }
}
