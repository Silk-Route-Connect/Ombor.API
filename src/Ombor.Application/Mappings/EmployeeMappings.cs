using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class EmployeeMappings
{
    public static EmployeeDto ToDto(this Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Salary,
            employee.PhoneNumber,
            employee.Email,
            employee.Address,
            employee.Description,
            employee.Role.ToString(),
            employee.Access.ToString(),
            employee.Status.ToString(),
            employee.DateOfEmployment
        );

    public static Employee ToEntity(this CreateEmployeeRequest request) =>
        new()
        {
            FullName = request.FullName,
            Salary = request.Salary,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            Description = request.Description,
            Access = Enum.Parse<EmployeeAccess>(request.Access.ToString()),
            Role = Enum.Parse<EmployeeRole>(request.Role.ToString()),
            DateOfEmployment = request.DateOfEmployment,
        };

    public static CreateEmployeeResponse ToCreateResponse(this Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Salary,
            employee.PhoneNumber,
            employee.Email,
            employee.Address,
            employee.Description,
            employee.Role.ToString(),
            employee.Access.ToString(),
            employee.Status.ToString(),
            employee.DateOfEmployment
        );

    public static UpdateEmployeeResponse ToUpdateResponse(this Employee employee) =>
       new(
        employee.Id,
        employee.FullName,
        employee.Salary,
        employee.PhoneNumber,
        employee.Email,
        employee.Address,
        employee.Description,
        employee.Role.ToString(),
        employee.Access.ToString(),
        employee.Status.ToString(),
        employee.DateOfEmployment
       );

    public static void ApplyUpdate(this Employee employee, UpdateEmployeeRequest request)
    {
        employee.FullName = request.FullName;
        employee.Salary = request.Salary;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Email = request.Email;
        employee.Address = request.Address;
        employee.Description = request.Description;
        employee.Role = Enum.Parse<EmployeeRole>(request.Role.ToString());
        employee.Access = Enum.Parse<EmployeeAccess>(request.Access.ToString());
        employee.Status = Enum.Parse<EmployeeStatus>(request.Status.ToString());
        employee.DateOfEmployment = request.DateOfEmployment;
    }
}
