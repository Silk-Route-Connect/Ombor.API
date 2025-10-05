using System.Globalization;
using Ombor.Contracts.Common;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Tests.Common.Factories;

public static class EmployeeRequestFactory
{
    private const int DefaultEmployeeId = 12;
    private static readonly CultureInfo _culture = new("uz-UZ");

    public static CreateEmployeeRequest GenerateValidCreateRequest()
        => new(
            FullName: "Test Employee Name",
            Position: "Test Employee Position",
            Salary: 1_000,
            Status: EmployeeStatus.Active,
            DateOfEmployment: DateOnly.Parse("2023-01-01", _culture),
            ContactInfo: GenerateValidContactInfo());

    public static CreateEmployeeRequest GenerateInvalidCreateRequest()
        => new(
            FullName: "",
            Position: "",
            Salary: -5_000,
            Status: EmployeeStatus.Active,
            DateOfEmployment: DateOnly.Parse("2023-01-01", _culture),
            ContactInfo: GenerateInvalidContactInfo());

    public static UpdateEmployeeRequest GenerateValidUpdateRequest(int? employeeId = null)
        => new(
            Id: employeeId ?? DefaultEmployeeId,
            FullName: "Updated Employee Name",
            Position: "Updated Emplloyee Position",
            Salary: 50_000,
            Status: EmployeeStatus.OnVacation,
            DateOfEmployment: DateOnly.Parse("2023-01-01", _culture),
            ContactInfo: GenerateValidContactInfo());

    public static UpdateEmployeeRequest GenerateInvalidUpdateRequest(int? employeeId = null)
        => new(
            Id: employeeId ?? DefaultEmployeeId,
            FullName: "",
            Position: "",
            Salary: -5_000,
            Status: EmployeeStatus.Active,
            DateOfEmployment: DateOnly.Parse("2023-01-01", _culture),
            ContactInfo: GenerateInvalidContactInfo());

    private static ContactInfo GenerateValidContactInfo()
        => new(
            PhoneNumbers: ["+99890-000-00-00"],
            Email: "john@mail.com",
            Address: "Test Employee Address",
            TelegramAccount: "@john_telegram");

    private static ContactInfo GenerateInvalidContactInfo()
        => new(
            PhoneNumbers: ["+99890-000-ss-aa"],
            Email: "johnmail.com",
            Address: "Test Employee Address",
            TelegramAccount: "john_telegram");
}
