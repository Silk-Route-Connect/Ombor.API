using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class GetEmployeesTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    private const string _matchingSearchTerm = "Test FullName";
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
            new()
            {
                FullName=searchTerm,
                Salary=1000,
                PhoneNumber="",
                Email="",
                Address="",
                Description="",
                Position=EmployeePosition.Viewer,
                Status=EmployeeStatus.Active,
                DateOfEmployment= DateOnly.FromDateTime(DateTime.UtcNow)
            },

            new()
            {
                FullName="John",
                Salary=1000,
                PhoneNumber="",
                Email="",
                Address=searchTerm,
                Description="",
                Position=EmployeePosition.Viewer,
                Status=EmployeeStatus.Active,
                DateOfEmployment= DateOnly.FromDateTime(DateTime.UtcNow)
            },

            new()
            {
                FullName=string.Empty,
                Salary=1000,
                PhoneNumber="",
                Email="",
                Address="",
                Description="",
                Position=EmployeePosition.Viewer,
                Status=EmployeeStatus.Active,
                DateOfEmployment= DateOnly.FromDateTime(DateTime.UtcNow)
            },

            new()
            {
                FullName="          ",
                Salary=1000,
                PhoneNumber="",
                Email="",
                Address="",
                Description="",
                Position=EmployeePosition.Viewer,
                Status=EmployeeStatus.Active,
                DateOfEmployment= DateOnly.FromDateTime(DateTime.UtcNow)
            },
        };

        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();
    }
}
