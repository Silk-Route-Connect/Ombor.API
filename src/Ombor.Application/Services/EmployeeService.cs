using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class EmployeeService(IApplicationDbContext context, IRequestValidator validator) : IEmployeeService
{
    public async Task<EmployeeDto[]> GetAsync(GetEmployeesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.FullName.Contains(request.SearchTerm) ||
                                x.Role.Contains(request.SearchTerm));
        }

        return await query
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new EmployeeDto(x.Id, x.FullName, x.Role, x.IsActive))
            .ToArrayAsync();
    }

    public async Task<EmployeeDto> GetByIdAsync(GetEmployeeByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var employee = await GetOrThrowAsync(request.Id);

        return employee.ToDto();
    }

    public async Task<CreateEmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        context.Employees.Add(entity);
        await context.SaveChangesAsync();

        return entity.ToCreateResponse();
    }

    public async Task<UpdateEmployeeResponse> UpdateAsync(UpdateEmployeeRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var employee = await GetOrThrowAsync(request.Id);

        employee.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return employee.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteEmployeeRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var employee = await GetOrThrowAsync(request.Id);

        context.Employees.Remove(employee);
        await context.SaveChangesAsync();
    }

    private async Task<Employee> GetOrThrowAsync(int id) =>
        await context.Employees.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Employee>(id);
}