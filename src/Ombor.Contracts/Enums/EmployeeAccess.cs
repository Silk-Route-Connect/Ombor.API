namespace Ombor.Contracts.Enums;
public enum EmployeeAccess
{
    /// <summary>
    /// Can manage sales and close customer debts.
    /// </summary>
    SalesAccess = 1,

    /// <summary>
    /// Can manage supplies and settle supplier debts.
    /// </summary>
    SuppliesAccess = 2,

    /// <summary>
    /// Can view and close customer debts.
    /// </summary>
    CustomerDebtsAccess = 3,

    /// <summary>
    /// Can View and close supplier debts.
    /// </summary>
    SupplierDebtsAccess = 4,

    /// <summary>
    /// Full access to all application features.
    /// </summary>
    FullAccess = 5,
}
