using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// A single product line of a <see cref="Transfer"/>.
/// </summary>
public class TransferLine : EntityBase, ITenantScoped
{
    public int TenantId { get; set; }

    public decimal Quantity { get; set; }

    public int ProductId { get; set; }
    public virtual required Product Product { get; set; }

    public int TransferId { get; set; }
    public virtual required Transfer Transfer { get; set; }
}
