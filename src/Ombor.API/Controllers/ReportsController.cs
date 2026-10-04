using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Report;
using Ombor.Contracts.Responses.Report;

namespace Ombor.API.Controllers;

/// <summary>
/// Reports («Отчёты»): read-only aggregates over the event ledger for the caller's organization. Periods are local
/// (Tashkent) calendar days, <c>from</c>–<c>to</c> inclusive, defaulting to the current month.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    /// <summary>Sales and sale refunds with cost of goods sold and gross profit, by day/week/month or by product/category/partner/warehouse.</summary>
    /// <param name="request">The period and grouping.</param>
    /// <returns>The rows, the totals and whether any cost is estimated.</returns>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalesReportDto>> GetSalesAsync([FromQuery] GetSalesReportRequest request)
    {
        var response = await service.GetSalesAsync(request);

        return Ok(response);
    }

    /// <summary>Supplies and supply refunds, by day/week/month or by product/category/partner/warehouse.</summary>
    /// <param name="request">The period and grouping.</param>
    /// <returns>The rows and the totals.</returns>
    [HttpGet("purchases")]
    [ProducesResponseType(typeof(PurchasesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PurchasesReportDto>> GetPurchasesAsync([FromQuery] GetPurchasesReportRequest request)
    {
        var response = await service.GetPurchasesAsync(request);

        return Ok(response);
    }

    /// <summary>Stock on hand now per product per warehouse, valued at cost and at sale price, with low-stock flags.</summary>
    /// <param name="request">An optional warehouse.</param>
    /// <returns>The rows, per-warehouse totals and overall totals.</returns>
    [HttpGet("stock")]
    [ProducesResponseType(typeof(StockReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockReportDto>> GetStockAsync([FromQuery] GetStockReportRequest request)
    {
        var response = await service.GetStockAsync(request);

        return Ok(response);
    }

    /// <summary>Wallet balances at the start and end of the period, money in and out by payment type, and a daily series.</summary>
    /// <param name="request">The period and an optional wallet.</param>
    /// <returns>Per-wallet figures, the payment-type breakdown, the daily series and the totals.</returns>
    [HttpGet("cash-flow")]
    [ProducesResponseType(typeof(CashFlowReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CashFlowReportDto>> GetCashFlowAsync([FromQuery] GetCashFlowReportRequest request)
    {
        var response = await service.GetCashFlowAsync(request);

        return Ok(response);
    }

    /// <summary>Money paid out in the period by payment type, partner, employee (payroll) and description (other expenses).</summary>
    /// <param name="request">The period.</param>
    /// <returns>The expense breakdowns and the total.</returns>
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ExpensesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpensesReportDto>> GetExpensesAsync([FromQuery] GetReportPeriodRequest request)
    {
        var response = await service.GetExpensesAsync(request);

        return Ok(response);
    }

    /// <summary>Stock written off by decrease adjustments in the period, by product and by reason.</summary>
    /// <param name="request">The period.</param>
    /// <returns>The losses and their total value.</returns>
    [HttpGet("losses")]
    [ProducesResponseType(typeof(LossesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LossesReportDto>> GetLossesAsync([FromQuery] GetReportPeriodRequest request)
    {
        var response = await service.GetLossesAsync(request);

        return Ok(response);
    }

    /// <summary>A simple profit and loss by day/week/month: net revenue − COGS − losses − payroll − other expenses.</summary>
    /// <param name="request">The period and grouping.</param>
    /// <returns>The rows, the totals and whether any cost is estimated.</returns>
    [HttpGet("profit")]
    [ProducesResponseType(typeof(ProfitReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfitReportDto>> GetProfitAsync([FromQuery] GetProfitReportRequest request)
    {
        var response = await service.GetProfitAsync(request);

        return Ok(response);
    }
}
