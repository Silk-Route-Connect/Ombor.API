using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Tests.Common.Factories;

public static class EmployeeRequestFactory
{
    private const int DefaultEmployeeId = 12;

    public static CreateEmployeeRequest GenerateValidCreateRequest()
        => new(
            FullName: "Test Employee Name",
            Salary: 1000,
            PhoneNumber: "+99890-000-00-00",
            Email: "employeeTestemail@test.com",
            Address: "Test employee address",
            Description: "Test employee description",
            Position: EmployeePosition.Admin,
            DateOfEmployment: DateOnly.Parse("2023-01-01")
            );

    public static CreateEmployeeRequest GenerateInvalidCreateRequest()
        => new(
            FullName: "",
            Salary: 1000,
            PhoneNumber: "+99890-000-ss-aa",
            Email: "employeeTestemail@test.com",
            Address: "Test employee address",
            Description: "Test employee description",
            Position: EmployeePosition.Admin,
            DateOfEmployment: DateOnly.Parse("2023-01-01")
            );

    public static UpdateEmployeeRequest GenerateValidUpdateRequest(int? employeeId = null)
        => new(
            Id: employeeId ?? DefaultEmployeeId,
            FullName: "Test employee",
            Salary: 1000,
            PhoneNumber: "+99890-000-00-00",
            Email: "employeeTestemail@test.com",
            Address: "Test employee address",
            Description: "Test employee description",
            Position: EmployeePosition.Admin,
            Status: EmployeeStatus.Active,
            DateOfEmployment: DateOnly.Parse("2023-01-01"));

    public static UpdateEmployeeRequest GenerateInvalidUpdateRequest(int? employeeId = null)
        => new(
            Id: employeeId ?? DefaultEmployeeId,
            FullName: "",
            Salary: 1000,
            PhoneNumber: "+99890-000-ss-aa",
            Email: "employeeTestemail@test.com",
            Address: "Test employee address",
            Description: "Test employee description",
            Position: EmployeePosition.Admin,
            Status: EmployeeStatus.Active,
            DateOfEmployment: DateOnly.Parse("2023-01-01")
        );
}
