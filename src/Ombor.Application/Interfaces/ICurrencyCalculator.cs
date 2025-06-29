namespace Ombor.Application.Interfaces;

internal interface ICurrencyCalculator
{
    decimal CalculateLocalAmount(decimal amount, decimal exchangeRate);
}
