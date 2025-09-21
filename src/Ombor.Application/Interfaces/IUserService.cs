using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.User;
using Ombor.Contracts.Responses.Common;
using Ombor.Contracts.Responses.User;

namespace Ombor.Application.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserDto>> GetUsersAsync(int organizationId, PagedRequest request);
    Task<UserDto> GetUserByIdAsync(int userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, int organizationId, int createdBy);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, int organizationId);
    Task<bool> DeactivateUserAsync(int userId, int organizationId);
    Task<bool> ActivateUserAsync(int userId, int organizationId);
    Task<bool> AssignRoleAsync(int userId, int roleId, int organizationId, int assignedBy);
    Task<bool> RemoveRoleAsync(int userId, int roleId, int organizationId);
}
