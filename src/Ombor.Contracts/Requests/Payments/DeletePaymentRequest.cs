namespace Ombor.Contracts.Requests.Payments;

/// <summary>
/// Request to delete a payment.
/// </summary>
/// <param name="Id">ID of the payment to delete.</param>
public sealed record DeletePaymentRequest(int Id);
