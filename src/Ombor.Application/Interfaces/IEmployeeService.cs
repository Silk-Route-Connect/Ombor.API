using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;

namespace Ombor.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedList<EmployeeDto>> GetAsync(GetEmployeesRequest request);
    Task<EmployeeDto> GetByIdAsync(GetEmployeeByIdRequest request);
    Task<CreateEmployeeResponse> CreateAsync(CreateEmployeeRequest request);
    Task<UpdateEmployeeResponse> UpdateAsync(UpdateEmployeeRequest request);
    Task DeleteAsync(DeleteEmployeeRequest request);
}