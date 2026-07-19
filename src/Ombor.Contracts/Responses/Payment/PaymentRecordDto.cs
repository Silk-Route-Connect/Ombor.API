namespace Ombor.Contracts.Responses.Payment;

/// <summary>
/// A payment in the source/allocation model (rules 8-14). <see cref="Sources"/> is where the
/// money came from; <see cref="Allocations"/> is what it settled. All money figures are server-computed.
/// </summary>
/// <param name="TransactionNotes">
/// Echoed read-only from the transaction(s) this payment settles: the note captured when a transaction and
/// its payment are created together belongs to either — we can't tell upfront — so it is surfaced on both.
/// The single settled transaction's note (the first, if the payment settles several); null when none.
/// </param>
/// <param name="TransactionAttachments">
/// Echoed read-only: the attachments of the transaction(s) this payment settles (union when it settles more
/// than one); empty when none. The files are stored on the transaction — not duplicated onto the payment.
/// </param>
public sealed record PaymentRecordDto(
    int Id,
    string? Number,
    DateTimeOffset Date,
    string Type,
    string Direction,
    int? PartnerId,
    string? PartnerName,
    string? PartnerType,
    int? EmployeeId,
    string? EmployeeName,
    string? EmployeePosition,
    int? WalletId,
    string? WalletName,
    string? WalletType,
    decimal Amount,
    string AllocationSummary,
    string? Description,
    string? Period,
    decimal? Salary,
    string? CreatedBy,
    PaymentSourceDto[] Sources,
    PaymentAllocationEntryDto[] Allocations,
    PaymentAttachmentDto[] Attachments,
    string? TransactionNotes,
    PaymentAttachmentDto[] TransactionAttachments);

/// <summary>
/// A file uploaded with a payment. Raw values only — the client derives the icon from
/// <see cref="ContentType"/> and formats <see cref="SizeBytes"/> for display. Mirrors the transaction attachment shape.
/// </summary>
/// <param name="Id">The attachment id.</param>
/// <param name="Name">The original file name as uploaded.</param>
/// <param name="ContentType">The MIME type (e.g. application/pdf, image/jpeg).</param>
/// <param name="SizeBytes">The file size in bytes.</param>
/// <param name="Url">The public URL to fetch the file.</param>
public sealed record PaymentAttachmentDto(
    int Id,
    string Name,
    string ContentType,
    long SizeBytes,
    string Url);

/// <summary>The source side of a payment (rule 9): a wallet draw or a draw against the partner's advance.</summary>
public sealed record PaymentSourceDto(
    int Id,
    string SourceType,
    int? WalletId,
    string? WalletName,
    string? WalletType,
    decimal Amount);

/// <summary>
/// The destination side of a payment (rule 10): what the money settled. <see cref="TransactionId"/>
/// links a <c>TransactionSettlement</c> to the transaction it paid (null for advance/change); the
/// frontend composes any display label from <see cref="AllocationType"/> + <see cref="TransactionId"/>.
/// </summary>
/// <param name="TransactionType">
/// The settled transaction's type (Sale, Supply, SaleRefund, SupplyRefund); null for advance/change
/// allocations. Lets the frontend pick the right detail route (sales vs supplies) for a settlement row.
/// </param>
public sealed record PaymentAllocationEntryDto(
    int Id,
    string AllocationType,
    int? TransactionId,
    string? TransactionType,
    decimal Amount);
