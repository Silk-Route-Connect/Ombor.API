using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

/// <summary>
/// Multipart/form-data request that creates a payment with optional file attachments.
/// <para>
/// The request body must contain:
/// </para>
/// <list type="bullet">
///   <item><description>
///     A form part named <c>payload</c> containing the scalar fields below as JSON.
///   </description></item>
///   <item><description>
///     Zero or more form parts named <c>files</c> that hold the binary attachments
///     (for example scanned cheques or PDF slips).
///   </description></item>
/// </list>
/// </summary>
public sealed record CreatePaymentRequest(
    int? PartnerId,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentType Type,
    PaymentCurrency Currency,
    PaymentMethod Method,
    PaymentDirection Direction,
    IFormFile[]? Attachments);
