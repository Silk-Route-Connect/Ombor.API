using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface IOrganizationService
{
    Task<Organization> CreateAsync(string organizationName);
}
