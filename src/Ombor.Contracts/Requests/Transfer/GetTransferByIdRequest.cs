namespace Ombor.Contracts.Requests.Transfer;

/// <summary>
/// Request to retrieve a single transfer by its identifier.
/// </summary>
public sealed record GetTransferByIdRequest(int Id);
