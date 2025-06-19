namespace Ombor.Contracts.Requests.Payments;

/// <summary>
/// Request to fetch a payment by ID.
/// </summary>
/// <param name="Id">ID of the payment to fetch.</param>
public sealed record GetPaymentByIdRequest(int Id);
