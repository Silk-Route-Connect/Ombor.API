using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a template for transaction entities such as Sale and Supply.
/// </summary>
public class Template : AuditableEntity
{
    /// <summary>
    /// Gets or sets name of the <see cref="Template"/>.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets type of the <see cref="Template"/>
    /// </summary>
    public TemplateType Type { get; set; }

    /// <summary>
    /// Gets or sets items of the <see cref="Template"/>
    /// </summary>
    public virtual List<TemplateItem> Items { get; set; } = [];
}
