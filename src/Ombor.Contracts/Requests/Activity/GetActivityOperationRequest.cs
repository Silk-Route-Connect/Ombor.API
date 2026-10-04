namespace Ombor.Contracts.Requests.Activity;

/// <summary>One Activity Log operation with all of its changes.</summary>
/// <param name="OperationId">The operation id from a list item.</param>
public sealed record GetActivityOperationRequest(Guid OperationId);
