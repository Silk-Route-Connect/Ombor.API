namespace Ombor.Contracts.Responses.Payment;

/// <summary>
/// Metadata for a file linked to a payment.
/// The binary content is never returned by the API.
/// </summary>
public sealed record PaymentAttachmentDto(
    int Id,
    int PaymentId,
    string FileName,
    string FileUrl,
    string? Description);
