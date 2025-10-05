using System.Globalization;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class GetEmployeesTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    private const string _matchingSearchTerm = "Test FullName";
    private static readonly CultureInfo _culture = new("uz-UZ");

    public static TheoryData<GetEmployeesRequest> Requests =>
        new()
        {
            new GetEmployeesRequest(),
            new GetEmployeesRequest("   "),
            new GetEmployeesRequest("Test Template"),
            new GetEmployeesRequest(null),
        };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task GetAsync_ShouldReturnMatchingEmployees(GetEmployeesRequest request)
    {
        // Arrange
        await CreateEmployeesAsync(request);
        var url = GetUrl(request);

        // Act 
        var response = await _client.GetAsync<EmployeeDto[]>(url);

        // Assert
        await _responseValidator.Employee.ValidateGetAsync(request, response);
    }

    private async Task CreateEmployeesAsync(GetEmployeesRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSearchTerm;

        var employees = new List<Employee>
        {
            // Matching name
            new()
            {
                FullName = searchTerm,
                Position = "Salesman",
                Salary = 5_000,
                Status = Domain.Enums.EmployeeStatus.Active,
                DateOfEmployment = DateOnly.Parse("2020-01-01", _culture),
            },
            // Matching position
            new()
            {
                FullName = "Test Employee for Search - 1",
                Position = searchTerm,
                Salary = 10_000,
                Status = Domain.Enums.EmployeeStatus.Active,
                DateOfEmployment = DateOnly.Parse("2019-02-02", _culture)
            },
            // Matching phone number
            new()
            {
                FullName = "Test Employee for Search - 2",
                Position = "Manager",
                Salary = 15_000,
                Status = Domain.Enums.EmployeeStatus.Active,
                DateOfEmployment = DateOnly.Parse("2018-03-03", _culture),
                ContactInfo = new()
                {
                    PhoneNumbers = [searchTerm],
                }
            },
            // Matching email
            new()
            {
                FullName = "Test Employee for Search - 3",
                Position = "Driver",
                Salary = 20_000,
                Status = Domain.Enums.EmployeeStatus.Active,
                DateOfEmployment = DateOnly.Parse("2017-04-04", _culture),
                ContactInfo = new()
                {
                    PhoneNumbers = ["+998901234567"],
                    Email = searchTerm + "@example.com",
                }
            },
            // Non-matching
            new()
            {
                FullName = "Non-matching Employee",
                Position = "Cleaner",
                Salary = 2_000,
                Status = Domain.Enums.EmployeeStatus.Active,
                DateOfEmployment = DateOnly.Parse("2016-05-05", _culture),
                ContactInfo = new()
                {
                    PhoneNumbers = ["+998909876543"],
                    Email = "user@test.com",
                }
            }
        };

        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();
    }
}
