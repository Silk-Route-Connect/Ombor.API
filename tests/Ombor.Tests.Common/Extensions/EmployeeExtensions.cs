using Ombor.Contracts.Requests.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Extensions;

public static class EmployeeExtensions
{
    public static bool IsEquivalent(this Employee employee, CreateEmployeeRequest request) =>
        employee.FullName == request.FullName &&
        employee.Salary == request.Salary &&
        employee.PhoneNumber == request.PhoneNumber &&
        employee.Email == request.Email &&
        employee.Address == request.Address &&
        employee.Description == request.Description &&
        employee.Position == Enum.Parse<EmployeePosition>(request.Position.ToString());
}
