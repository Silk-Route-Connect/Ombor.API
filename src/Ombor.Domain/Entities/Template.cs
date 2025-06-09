using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Template : AuditableEntity
{
    public required string Name { get; set; }
    public TemplateType Type { get; set; }

    public virtual ICollection<TemplateItem> Items { get; set; }

    public Template()
    {
        Items = new HashSet<TemplateItem>();
    }
}
