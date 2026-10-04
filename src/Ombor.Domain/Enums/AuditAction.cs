namespace Ombor.Domain.Enums;

public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,

    /// <summary>An update that set the archive flag.</summary>
    Archived = 4,

    /// <summary>An update that cleared the archive flag.</summary>
    Restored = 5,
}
