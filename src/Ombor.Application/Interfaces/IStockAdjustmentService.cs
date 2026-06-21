using FluentValidation;
using Ombor.Contracts.Requests.StockAdjustment;
using Ombor.Contracts.Responses.StockAdjustment;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Stock adjustments are immutable — created, never edited or deleted (corrections are counter-events, rule 1).
/// </summary>
public interface IStockAdjustmentService
{
    /// <summary>Lists stock adjustments (newest-first), optionally filtered by warehouse and/or product.</summary>
    Task<StockAdjustmentDto[]> GetAsync(GetStockAdjustmentsRequest request);

    /// <summary>Records a stock adjustment and moves stock atomically.</summary>
    /// <exception cref="ValidationException">If validation fails or a decrease would drive stock below zero.</exception>
    Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request);
}
