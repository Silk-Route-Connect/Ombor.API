using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class EmployeeService(IApplicationDbContext context, IRequestValidator validator) : IEmployeeService
{
    public async Task<PagedList<EmployeeDto>> GetAsync(GetEmployeesRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var employees = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var employeesDto = employees.Select(x => x.ToDto());

        return PagedList<EmployeeDto>.ToPagedList(employeesDto, totalCount, request.PageNumber, request.PageSize);
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

    private IQueryable<Employee> GetQuery(GetEmployeesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Employees.AsNoTracking();
        var searchTerm = request.SearchTerm;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.FullName.Contains(searchTerm) ||
                x.Position.Contains(searchTerm) ||
                (x.ContactInfo != null && x.ContactInfo.PhoneNumbers.Contains(searchTerm)));
        }

        if (request.Status.HasValue)
        {
            var status = Enum.Parse<EmployeeStatus>(request.Status.Value.ToString());
            query = query.Where(x => x.Status == status);
        }

        return query;
    }

    private IQueryable<Employee> ApplySort(IQueryable<Employee> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "name_asc" => query.OrderBy(x => x.FullName),
            "name_desc" => query.OrderByDescending(x => x.FullName),
            "position_asc" => query.OrderBy(x => x.Position),
            "position_desc" => query.OrderByDescending(x => x.Position),
            "salary_asc" => query.OrderBy(x => x.Salary),
            "salary_desc" => query.OrderByDescending(x => x.Salary),
            "status_asc" => query.OrderBy(x => x.Status),
            "status_desc" => query.OrderByDescending(x => x.Status),
            "hiredate_asc" => query.OrderBy(x => x.DateOfEmployment),
            _ => query.OrderByDescending(x => x.DateOfEmployment),
        };
}