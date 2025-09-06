using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class EmployeeTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "John Doe";

    protected override string GetUrl()
        => Routes.Employee;

    protected override string GetUrl(int id)
        => $"{Routes.Employee}/{id}";

    protected async Task<int> CreateEmployeeAsync()
    {
        var employee = new Employee
        {
            FullName = "John Doe",
            PhoneNumber = "+99890-000-00-00",
            Position = EmployeePosition.Admin,
            Status = EmployeeStatus.Active,
            DateOfEmployment = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee.Id;
    }

    protected async Task<int> CreateEmployeeAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee.Id;
    }
}
