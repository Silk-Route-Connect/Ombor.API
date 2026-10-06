namespace Ombor.Domain.Common;

/// <summary>
/// Marks an entity whose every change (insert, update, archive, delete) is recorded in the audit log that backs
/// the Activity Log (rules 26–28): the money and stock events and the mutable master data. The audit interceptor
/// writes the rows; an entity never writes its own. Properties that must never reach the log are marked
/// <see cref="NotAuditedAttribute"/>.
/// </summary>
public interface IAuditable;
