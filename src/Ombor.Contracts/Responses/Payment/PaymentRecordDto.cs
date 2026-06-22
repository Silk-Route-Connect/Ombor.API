namespace Ombor.Contracts.Responses.Payment;

/// <summary>
/// A payment in the source/allocation model (rules 8-14). <see cref="Sources"/> is where the
/// money came from; <see cref="Allocations"/> is what it settled. All money figures are server-computed.
/// </summary>
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
    PaymentAllocationEntryDto[] Allocations);

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
