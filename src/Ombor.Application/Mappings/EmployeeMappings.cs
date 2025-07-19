using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class EmployeeMappings
{
    public static EmployeeDto ToDto(this Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Role,
            employee.IsActive);

    public static Employee ToEntity(this CreateEmployeeRequest request) =>
        new()
        {
            FullName = request.FullName,
            Role = request.Role,
        };

    public static CreateEmployeeResponse ToCreateResponse(this Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Role,
            employee.IsActive);

    public static UpdateEmployeeResponse ToUpdateResponse(this Employee employee) =>
    new(
        employee.Id,
        employee.FullName,
        employee.Role,
        employee.IsActive);

    public static void ApplyUpdate(this Employee employee, UpdateEmployeeRequest request)
    {
        employee.FullName = request.FullName;
        employee.Role = request.Role;
        employee.IsActive = request.IsActive;
    }
}
