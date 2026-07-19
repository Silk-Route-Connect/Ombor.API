using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// A file uploaded with a payment (receipt, transfer confirmation, etc.). Stored with enough metadata
/// for the client to render it without re-reading the file: original name, MIME type, size, and URL.
/// Mirrors <see cref="TransactionAttachment"/>.
/// </summary>
public class PaymentAttachment : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>The storage key (generated file name) used to locate/delete the stored file.</summary>
    public required string FileId { get; set; }

    /// <summary>The original file name as uploaded; shown to the user.</summary>
    public required string FileName { get; set; }

    /// <summary>The MIME type (e.g. application/pdf, image/jpeg); the client derives its icon from it.</summary>
    public required string ContentType { get; set; }

    /// <summary>The file size in bytes; the client formats it for display.</summary>
    public long SizeBytes { get; set; }

    /// <summary>The public URL to fetch the file.</summary>
    public required string Url { get; set; }

    public int PaymentId { get; set; }
    public virtual required Payment Payment { get; set; }
}
