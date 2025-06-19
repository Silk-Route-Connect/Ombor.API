using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a partner entity.
/// </summary>
public class Partner : EntityBase
{
    /// <summary>Gets or sets the name of the partner.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the address of the partner.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the email address of the partner.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the name of the company associated with the partner.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Gets or sets the partner's balance.</summary>
    public decimal Balance { get; set; }

    /// <summary>Gets or sets the type of the Partner.</summary>
    public PartnerType Type { get; set; }

    /// <summary>Gets or sets the partner's phone numbers.</summary>
    public List<string> PhoneNumbers { get; set; } = [];

    public virtual ICollection<TransactionRecord> Transactions { get; set; } = [];
}
