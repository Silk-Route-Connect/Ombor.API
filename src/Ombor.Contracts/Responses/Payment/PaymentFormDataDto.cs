namespace Ombor.Contracts.Responses.Payment;

/// <summary>Reference data for the payment-create form: who can be paid and which wallets exist.</summary>
public sealed record PaymentFormDataDto(
    PaymentFormPartnerDto[] Partners,
    PaymentFormEmployeeDto[] Employees,
    PaymentFormWalletDto[] Wallets);

/// <param name="Balance">Net partner balance (+ owes us, − we owe).</param>
/// <param name="Advance">The partner's available advance claim.</param>
public sealed record PaymentFormPartnerDto(int Id, string Name, string Type, decimal Balance, decimal Advance);

public sealed record PaymentFormEmployeeDto(int Id, string Name, string Position, decimal Salary);

/// <param name="Balance">The wallet's computed balance.</param>
public sealed record PaymentFormWalletDto(int Id, string Name, string Type, decimal Balance);
