namespace Ombor.Contracts.Enums;

/// <summary>Physical channel through which money moved.</summary>
public enum PaymentMethod
{
    Unknown = 0,

    Cash = 1,

    Card = 2,

    BankTransfer = 3,

    AccountBalance = 4,
}
