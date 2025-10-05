using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class EmployeeValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetEmployeesRequest request, EmployeeDto[] response)
    {
        var expectedEmployees = await GetEmployeesAsync(request);

        Assert.Equal(expectedEmployees.Length, response.Length);
        Assert.All(expectedEmployees, expected =>
        {
            var actual = response.FirstOrDefault(x => x.Id == expected.Id);

            EmployeeAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int employeeId, EmployeeDto response)
    {
        var expected = await context.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        EmployeeAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateEmployeeRequest request, CreateEmployeeResponse response)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(x => x.Id == response.Id);

        EmployeeAssertionHelper.AssertEquivalent(request, response);
        EmployeeAssertionHelper.AssertEquivalent(request, employee);
        EmployeeAssertionHelper.AssertEquivalent(employee, response);
    }

    public async Task ValidatePutAsync(UpdateEmployeeRequest request, UpdateEmployeeResponse response)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        EmployeeAssertionHelper.AssertEquivalent(request, response);
        EmployeeAssertionHelper.AssertEquivalent(request, employee);
        EmployeeAssertionHelper.AssertEquivalent(employee, response);
    }

    public async Task ValidateDeleteAsync(int employeeId)
    {
        var employee = await context.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        Assert.Null(employee);
    }

    private async Task<Employee[]> GetEmployeesAsync(GetEmployeesRequest request)
    {
        var searchTerm = request.SearchTerm?.Trim();
        var query = context.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.FullName.Contains(searchTerm) ||
                x.Position.Contains(searchTerm) ||
                (x.ContactInfo != null && x.ContactInfo.PhoneNumbers.Contains(searchTerm)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .ToArrayAsync();
    }
}
