using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class LedgerEntry : EntityBase
{
    public required DateTimeOffset CreatedAtUtc { get; set; }
    public required decimal AmountLocal { get; set; }
    public string? Notes { get; set; }
    public required LedgerType Type { get; set; }

    public required string Source { get; set; }
    public required int SourceId { get; set; }

    public int PartnerId { get; set; }
    public virtual required Partner Partner { get; set; }
}
