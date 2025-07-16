using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Interfaces;

public interface IEmployeeBuilder
{
    IEmployeeBuilder WithId(int? id = null);
    IEmployeeBuilder WithFullName(string? fullName = null);
    IEmployeeBuilder WithRole(string? role = null);
    IEmployeeBuilder WithIsActive(bool? isActive = null);
    Employee Build();
    Employee BuildAndPopulate();
}
