using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class OtpCode : AuditableEntity
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
}
