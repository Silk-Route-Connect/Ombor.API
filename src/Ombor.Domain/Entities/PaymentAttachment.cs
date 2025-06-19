using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Binary file (scan, photo, PDF) linked to a payment.
/// </summary>
public class PaymentAttachment : EntityBase
{
    /// <summary>
    /// Original file name (as uploaded by the user).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Absolute or relative URL/path where the file is stored.
    /// </summary>
    public required string FileUrl { get; set; }

    /// <summary>
    /// Optional human-readable comment.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// FK to the owning payment.
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Navigation to the owning payment.
    /// </summary>
    public required virtual Payment Payment { get; set; }
}
