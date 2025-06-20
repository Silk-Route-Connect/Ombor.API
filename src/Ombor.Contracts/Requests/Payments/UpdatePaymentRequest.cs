using Microsoft.AspNetCore.Http;
using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

/// <summary>
/// Multipart/form-data request that updates an existing payment.
/// <para>
/// The request body must contain:
/// </para>
/// <list type="bullet">
///   <item><description>
///     A form part named <c>payload</c> containing the scalar fields below as JSON.
///   </description></item>
///   <item><description>
///     <c>AttachmentsToAdd</c> should contain the new files to store.
///   </description></item>
///   <item><description>
///    <c>AttachmentsToDelete</c> lists IDs of existing attachments to remove.
///   </description></item>
/// </list>
/// </summary>
public sealed record UpdatePaymentRequest(
    int Id,
    int PaymentId,
    int? PartnerId,
    string? Notes,
    decimal Amount,
    decimal ExchangeRate,
    PaymentType Type,
    PaymentCurrency Currency,
    PaymentMethod Method,
    IFormFile[]? AttachmentsToAdd,
    int[]? AttachmentsToDelete);
