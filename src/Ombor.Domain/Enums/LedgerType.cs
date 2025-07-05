namespace Ombor.Domain.Enums;

public enum LedgerType
{
    InvoiceCreated = 1,
    InvoicePaid = 2,
    InvoicePaidUsingBalance = 3, // It is only for internal use
    BalanceDeposit = 4,
    BalanceWithdrawal = 5,
    ManualAdjustment = 10
}
