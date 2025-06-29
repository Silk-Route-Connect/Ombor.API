using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class CurrencyCalculator : ICurrencyCalculator
{
    public decimal CalculateLocalAmount(decimal amount, decimal exchangeRate)
    {
        if (amount < 0)
        {
            throw new ArgumentException($"Amount must be positive.", nameof(amount));
        }

        if (exchangeRate <= 0)
        {
            throw new ArgumentException("Exchange rate must be positive and greater than 0.", nameof(exchangeRate));
        }

        return amount * exchangeRate;
    }
}
