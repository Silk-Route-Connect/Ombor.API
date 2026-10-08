using Ombor.Contracts.Requests.Activity;
using Ombor.Contracts.Responses.Activity;

namespace Ombor.Application.Interfaces;

/// <summary>Reads the audit log as the Activity Log: filtered, paged operations of the current organization.</summary>
public interface IActivityService
{
    /// <summary>A page of operations matching the filters, newest first.</summary>
    Task<ActivityPageDto> GetAsync(GetActivityRequest request);

    /// <summary>One operation with all of its changes.</summary>
    /// <exception cref="Domain.Exceptions.EntityNotFoundException">No such operation in the organization.</exception>
    Task<ActivityItemDto> GetOperationAsync(GetActivityOperationRequest request);
}
