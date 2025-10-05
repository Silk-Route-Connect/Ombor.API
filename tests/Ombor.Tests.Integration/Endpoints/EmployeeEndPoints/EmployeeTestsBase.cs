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
            Position = "Test Employee Position",
            Salary = 5_000,
            Status = EmployeeStatus.Active,
            DateOfEmployment = DateOnly.FromDateTime(DateTime.UtcNow),
            ContactInfo = new Domain.Common.ContactInfo
            {
                PhoneNumbers = ["+998-90-123-45-67", "+99890-000-00-00"],
                Email = "test@mail.com",
                Address = "Test Address for Employee",
                TelegramAccount = "@test_telegram_account"
            }
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
