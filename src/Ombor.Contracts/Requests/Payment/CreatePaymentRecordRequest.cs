using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payment;

/// <summary>
/// Request to create a standalone payment. The money is drawn from <see cref="WalletId"/> and
/// distributed across <see cref="Settlements"/>; any excess becomes a partner advance, which is
/// only permitted when the partner has no remaining debt (rule 40).
/// </summary>
/// <param name="Type">Payment type (Transaction, Deposit, Withdrawal, Payroll, General).</param>
/// <param name="Direction">Whether money comes in (Income) or goes out (Expense).</param>
/// <param name="PartnerId">Required for Transaction, Deposit and Withdrawal.</param>
/// <param name="EmployeeId">Required for Payroll.</param>
/// <param name="WalletId">The wallet the money moves through.</param>
/// <param name="Amount">Total amount (must be greater than zero).</param>
/// <param name="Description">Required for General payments.</param>
/// <param name="Period">Payroll period, e.g. «2026-06».</param>
/// <param name="Settlements">Per-transaction amounts this payment settles.</param>
/// <param name="Attachments">Optional file attachments (receipts, transfer confirmations). Sent as multipart/form-data.</param>
public sealed record CreatePaymentRecordRequest(
    PaymentType Type,
    PaymentDirection Direction,
    int? PartnerId,
    int? EmployeeId,
    int WalletId,
    decimal Amount,
    string? Description,
    string? Period,
    SettlementInput[] Settlements,
    IFormFile[]? Attachments = null);

/// <summary>A single transaction-settlement instruction within a payment.</summary>
/// <param name="TransactionId">The transaction being settled.</param>
/// <param name="Amount">The amount applied to it (must not exceed its remaining).</param>
public sealed record SettlementInput(int TransactionId, decimal Amount);
