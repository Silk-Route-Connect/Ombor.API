using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;
public class RefreshToken : EntityBase
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public int UserId { get; set; }
    public virtual UserAccount User { get; set; } = default!;
}
