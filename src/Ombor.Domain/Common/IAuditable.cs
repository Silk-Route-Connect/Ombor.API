namespace Ombor.Domain.Common;

/// <summary>
/// Marks an entity whose every change (insert, update, delete) is a money- or
/// stock-affecting event and must be recorded in the audit log. Non-financial
/// entities (Product, Partner, Category, etc.) deliberately do not implement this.
/// </summary>
public interface IAuditable;
