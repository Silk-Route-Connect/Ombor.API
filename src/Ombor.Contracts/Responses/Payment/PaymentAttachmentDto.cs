namespace Ombor.Contracts.Responses.Payment;

public sealed record PaymentAttachmentDto(
    int Id,
    int PaymentId,
    string FileName,
    string FileUrl,
    string? Description);
