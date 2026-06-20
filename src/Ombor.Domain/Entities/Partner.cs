using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a partner entity.
/// </summary>
public class Partner : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>Gets or sets whether the partner is archived (hidden from default lists, restorable).</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the name of the partner.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the address of the partner.</summary>
    public string? Address { get; set; }

    /// <summary>Gets or sets the email address of the partner.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the name of the company associated with the partner.</summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// The partner's starting balance, recorded once at creation as an immutable event (signed:
    /// positive = the partner owes us). The net balance is computed from this plus the event log.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>The date the opening balance was recorded (partner creation). Immutable.</summary>
    public DateOnly OpeningDate { get; set; }

    /// <summary>Gets or sets the type of the Partner.</summary>
    public PartnerType Type { get; set; }

    /// <summary>Gets or sets the partner's phone numbers.</summary>
    public List<string> PhoneNumbers { get; set; } = [];

    /// <summary>
    /// Gets or sets the templates of the partner.
    /// </summary>
    public virtual ICollection<Template> Templates { get; set; } = [];

    /// <summary>
    /// Gets or sets the transactions of the partner.
    /// </summary>
    public virtual ICollection<TransactionRecord> Transactions { get; set; } = [];

    /// <summary>
    /// Gets or sets the payments of the partner.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = [];

    /// <summary>
    /// Gets or sets the orders of the partner.
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = [];
}
