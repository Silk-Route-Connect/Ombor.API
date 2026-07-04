using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a template for transaction entities such as Sale and Supply.
/// </summary>
public class Template : AuditableEntity, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets name of the <see cref="Template"/>.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets type of the <see cref="Template"/>.
    /// </summary>
    public TemplateType Type { get; set; }

    /// <summary>
    /// Gets or sets te Partner ID of the <see cref="Template"/>.
    /// </summary>
    public int PartnerId { get; set; }

    /// <summary>
    /// Gets or sets te Partner of the <see cref="Template"/>.
    /// </summary>
    public virtual required Partner Partner { get; set; }

    /// <summary>
    /// When the template was last loaded into a transaction; null until it is first used. Stamped by the
    /// "use" action so the list can show how recently each template was applied.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets items of the <see cref="Template"/>
    /// </summary>
    public virtual List<TemplateItem> Items { get; set; } = [];
}
