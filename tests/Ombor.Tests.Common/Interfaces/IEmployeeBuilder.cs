using Ombor.Domain.Common;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Interfaces;

public interface IEmployeeBuilder
{
    IEmployeeBuilder WithId(int? id = null);
    IEmployeeBuilder WithFullName(string? fullName = null);
    IEmployeeBuilder WithPosition(string? position = null);
    IEmployeeBuilder WithSalary(decimal? salary = null);
    IEmployeeBuilder WithStatus(EmployeeStatus? status = null);
    IEmployeeBuilder WithDateOfEmployment(DateOnly? dateOfEmployment = null);
    IEmployeeBuilder WithContactInfo(ContactInfo? contactInfo = null);
    Employee Build();
    Employee BuildAndPopulate();
}
