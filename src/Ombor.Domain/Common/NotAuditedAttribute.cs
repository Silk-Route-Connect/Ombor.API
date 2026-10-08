namespace Ombor.Domain.Common;

/// <summary>
/// Keeps a property's values out of the audit log: secrets (password hash and salt) and preferences that are not
/// business activity. With <see cref="MaskedAs"/>, a change is still recorded under that name, without values —
/// the log shows that the password changed, never what it is.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotAuditedAttribute : Attribute
{
    /// <summary>The field name a change is recorded under (values never stored); null records nothing.</summary>
    public string? MaskedAs { get; init; }
}
