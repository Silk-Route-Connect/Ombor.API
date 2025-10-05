using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class EmployeeMappings
{
    public static EmployeeDto ToDto(this Employee employee) =>
        new(
            Id: employee.Id,
            FullName: employee.FullName,
            Position: employee.Position,
            Status: employee.Status.ToString(),
            Salary: employee.Salary,
            DateOfEmployment: employee.DateOfEmployment,
            ContactInfo: employee.ContactInfo?.ToDto());

    public static Employee ToEntity(this CreateEmployeeRequest request) =>
        new()
        {
            FullName = request.FullName,
            Salary = request.Salary,
            Position = request.Position,
            DateOfEmployment = request.DateOfEmployment,
            Status = Enum.Parse<EmployeeStatus>(request.Status.ToString()),
            ContactInfo = request.ContactInfo?.ToEntity()
        };

    public static CreateEmployeeResponse ToCreateResponse(this Employee employee) =>
        new(
            Id: employee.Id,
            FullName: employee.FullName,
            Position: employee.Position,
            Status: employee.Status.ToString(),
            Salary: employee.Salary,
            DateOfEmployment: employee.DateOfEmployment,
            ContactInfo: employee.ContactInfo?.ToDto());

    public static UpdateEmployeeResponse ToUpdateResponse(this Employee employee) =>
       new(
           Id: employee.Id,
           FullName: employee.FullName,
           Position: employee.Position,
           Status: employee.Status.ToString(),
           Salary: employee.Salary,
           DateOfEmployment: employee.DateOfEmployment,
           ContactInfo: employee.ContactInfo?.ToDto());

    public static void ApplyUpdate(this Employee employee, UpdateEmployeeRequest request)
    {
        employee.FullName = request.FullName;
        employee.Position = request.Position;
        employee.Status = Enum.Parse<EmployeeStatus>(request.Status.ToString());
        employee.Salary = request.Salary;
        employee.DateOfEmployment = request.DateOfEmployment;
        employee.ContactInfo = request.ContactInfo?.ToEntity();
    }

    private static Domain.Common.ContactInfo? ToEntity(this Contracts.Common.ContactInfo? contactInfo) =>
        contactInfo is null
            ? null
            : new Domain.Common.ContactInfo
            {
                PhoneNumbers = contactInfo.PhoneNumbers,
                Email = contactInfo.Email,
                Address = contactInfo.Address,
                TelegramAccount = contactInfo.TelegramAccount
            };

    private static Contracts.Common.ContactInfo? ToDto(this Domain.Common.ContactInfo? contactInfo) =>
        contactInfo is null
            ? null
            : new Contracts.Common.ContactInfo(
                PhoneNumbers: contactInfo.PhoneNumbers,
                Email: contactInfo.Email,
                Address: contactInfo.Address,
                TelegramAccount: contactInfo.TelegramAccount);
}
