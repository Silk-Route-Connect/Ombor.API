using Ombor.Contracts.Requests.Employee;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Extensions;

public static class EmployeeExtensions
{
    public static bool IsEquivalent(this Employee employee, CreateEmployeeRequest request) =>
        employee.FullName == request.FullName &&
        employee.Role == request.Role;
}
