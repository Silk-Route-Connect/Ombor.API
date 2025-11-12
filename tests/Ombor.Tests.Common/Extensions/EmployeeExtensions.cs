using Ombor.Contracts.Requests.Employee;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class EmployeeExtensions
{
    public static bool IsEquivalent(this Employee employee, CreateEmployeeRequest request) =>
        employee.FullName == request.Name &&
        employee.Position == request.Position &&
        employee.Salary == request.Salary &&
        employee.DateOfEmployment == request.DateOfEmployment &&
        IsContactInfoEquivalent(employee.ContactInfo, request.ContactInfo);

    private static bool IsContactInfoEquivalent(Domain.Common.ContactInfo? domainContactInfo, Contracts.Common.ContactInfo? contractContactInfo)
        => domainContactInfo?.Email == contractContactInfo?.Email &&
           domainContactInfo?.Address == contractContactInfo?.Address &&
           domainContactInfo?.TelegramAccount == contractContactInfo?.TelegramAccount &&
           ((domainContactInfo?.PhoneNumbers == null && contractContactInfo?.PhoneNumbers == null) ||
            (domainContactInfo?.PhoneNumbers != null && contractContactInfo?.PhoneNumbers != null &&
             domainContactInfo.PhoneNumbers.Length == contractContactInfo.PhoneNumbers.Length &&
             !domainContactInfo.PhoneNumbers.Except(contractContactInfo.PhoneNumbers).Any()));
}
