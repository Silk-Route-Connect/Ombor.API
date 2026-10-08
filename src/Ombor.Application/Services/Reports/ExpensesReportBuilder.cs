using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Report;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// The expenses report: money paid out in a period by payment type, by partner (suppliers paid, refunds and advances
/// paid out), payroll by employee, and other (General) expenses grouped by their description — the only «статья» a
/// General payment carries today.
/// </summary>
internal sealed class ExpensesReportBuilder(IApplicationDbContext context, ReportExpenses expenses)
{
    private static readonly PaymentType[] PartnerTypes = [PaymentType.Transaction, PaymentType.Deposit, PaymentType.Withdrawal];

    public async Task<ExpensesReportDto> BuildAsync(ReportRange range)
    {
        var payments = await expenses.LoadAsync(range.StartUtc, range.EndUtc);

        var byType = Enum.GetValues<PaymentType>()
            .Select(type => new ExpenseTypeDto(
                type.ToContractType(),
                payments.Where(p => p.Type == type).Sum(p => p.Amount),
                payments.Count(p => p.Type == type)))
            .ToArray();

        var toPartners = payments
            .Where(p => p.PartnerId.HasValue && PartnerTypes.Contains(p.Type))
            .GroupBy(p => p.PartnerId!.Value)
            .ToArray();
        var partnerIds = toPartners.Select(g => g.Key).ToArray();
        var partnerNames = await context.Partners
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var payroll = payments
            .Where(p => p.EmployeeId.HasValue && p.Type == PaymentType.Payroll)
            .GroupBy(p => p.EmployeeId!.Value)
            .ToArray();
        var employeeIds = payroll.Select(g => g.Key).ToArray();
        var employeeNames = await context.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName);

        var other = payments
            .Where(p => p.Type == PaymentType.General)
            .GroupBy(p => p.Notes?.Trim().ToLowerInvariant() ?? string.Empty)
            .Select(g => new ExpenseOtherDto(
                g.Key.Length == 0 ? null : g.First().Notes!.Trim(),
                g.Sum(p => p.Amount),
                g.Count()))
            .OrderByDescending(o => o.Amount)
            .ThenBy(o => o.Description, StringComparer.Ordinal)
            .ToArray();

        return new ExpensesReportDto(
            range.From,
            range.To,
            payments.Sum(p => p.Amount),
            payments.Length,
            byType,
            [.. toPartners
                .Select(g => new ExpensePartnerDto(g.Key, partnerNames.GetValueOrDefault(g.Key, string.Empty), g.Sum(p => p.Amount), g.Count()))
                .OrderByDescending(p => p.Amount)
                .ThenBy(p => p.Name, StringComparer.Ordinal)],
            [.. payroll
                .Select(g => new ExpenseEmployeeDto(g.Key, employeeNames.GetValueOrDefault(g.Key, string.Empty), g.Sum(p => p.Amount), g.Count()))
                .OrderByDescending(e => e.Amount)
                .ThenBy(e => e.Name, StringComparer.Ordinal)],
            other);
    }
}
