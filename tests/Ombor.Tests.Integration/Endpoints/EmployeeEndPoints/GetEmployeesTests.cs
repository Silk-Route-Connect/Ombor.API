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
                Role="qwerty",
                IsActive=true
            },

            new()
            {
                FullName="John",
                Role=searchTerm,
                IsActive=false
            },

            new()
            {
                FullName=string.Empty,
                Role="qwerty",
                IsActive=true
            },

            new()
            {
                FullName="          ",
                Role="",
                IsActive=true
            },
        };

        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync();
    }
}
