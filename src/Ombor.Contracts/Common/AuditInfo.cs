namespace Ombor.Contracts.Common;

public sealed record AuditInfo(
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt,
    string? DeletedBy,
    DateTime? DeletedAt);
