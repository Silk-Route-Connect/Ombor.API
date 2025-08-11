using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Interfaces;

public interface IEmployeeBuilder
{
    IEmployeeBuilder WithId(int? id = null);
    IEmployeeBuilder WithFullName(string? fullName = null);
    IEmployeeBuilder WithSalary(decimal? salary = null);
    IEmployeeBuilder WithPhoneNumber(string? phoneNumber = null);
    IEmployeeBuilder WithEmail(string? email = null);
    IEmployeeBuilder WithAddress(string? address = null);
    IEmployeeBuilder WithDescription(string? description = null);
    IEmployeeBuilder WithPosition(EmployeePosition? position = null);
    IEmployeeBuilder WithStatus(EmployeeStatus? status = null);
    IEmployeeBuilder WithDateOfEmployment(DateOnly? dateOfEmployment = null);
    Employee Build();
    Employee BuildAndPopulate();
}
