using Ombor.Contracts.Requests.Employee;

namespace Ombor.Tests.Common.Factories;

public static class EmployeeRequestFactory
{
    private const int DefaultEmployeeId = 12;

    public static CreateEmployeeRequest GenerateValidCreateRequest()
        => new(
            "Test Employee",
            "Test employee role");

    public static CreateEmployeeRequest GenerateInvalidCreateRequest()
        => new(
            "",
            "      ");

    public static UpdateEmployeeRequest GenerateValidUpdateRequest(int? employeeId = null)
        => new(
            employeeId ?? DefaultEmployeeId,
            "Test Employee",
            "Test employee role",
            true);

    public static UpdateEmployeeRequest GenerateInvalidUpdateRequest(int? employeeId = null)
        => new(
            employeeId ?? DefaultEmployeeId,
            "       ",
            "",
            true);
}
