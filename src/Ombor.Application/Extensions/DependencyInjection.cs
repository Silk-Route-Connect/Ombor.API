using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Mappings;
using Ombor.Application.Services;
using Ombor.Application.Services.Activity;
using Ombor.Application.Services.Dashboard;
using Ombor.Application.Services.DebtPositions;
using Ombor.Application.Services.Reports;

namespace Ombor.Application.Extensions;

/// <summary>
/// Registers application‑layer services and FluentValidation adapters.
/// </summary>
public static class DependencyInjection
{
    private static Assembly CurrentAssembly => Assembly.GetExecutingAssembly();

    /// <summary>
    /// Adds product and category services and request validators to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(CurrentAssembly);
        services.AddConfigurations(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IBusinessClock, BusinessClock>();
        services.AddScoped<IRequestValidator, RequestValidator>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<ITransactionMapper, TransactionMapper>();
        services.AddScoped<TransactionCreateGuard>();
        services.AddScoped<TransactionPaymentBuilder>();
        services.AddScoped<TransactionStock>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<PaymentQueries>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<WalletQueries>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<ILoginThrottle, LoginThrottle>();
        services.AddScoped<IActiveUserCache, ActiveUserCache>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationSetupService, OrganizationSetupService>();
        services.AddScoped<IOtpCodeProvider, OtpCodeProvider>();
        services.AddScoped<OrderQueries>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IStockAdjustmentService, StockAdjustmentService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<DebtPositionCalculator>();
        services.AddScoped<IDebtService, DebtService>();
        services.AddScoped<DashboardSeriesBuilder>();
        services.AddScoped<DashboardMoney>();
        services.AddScoped<DashboardProfit>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ActivityQueries>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<ReportLines>();
        services.AddScoped<ReportGrouping>();
        services.AddScoped<ReportExpenses>();
        services.AddScoped<SalesReportBuilder>();
        services.AddScoped<PurchasesReportBuilder>();
        services.AddScoped<StockReportBuilder>();
        services.AddScoped<CashFlowReportBuilder>();
        services.AddScoped<ExpensesReportBuilder>();
        services.AddScoped<LossesReportBuilder>();
        services.AddScoped<ProfitReportBuilder>();
        services.AddScoped<IReportService, ReportService>();
        services.AddHttpClient();

        services.AddTransient<IFileService, FileService>();

        return services;
    }

    private static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileSettings>()
            .Bind(configuration.GetSection(FileSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmsSettings>()
            .Bind(configuration.GetSection(SmsSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CookieSettings>()
            .Bind(configuration.GetSection(CookieSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpSettings>()
            .Bind(configuration.GetSection(OtpSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthSecuritySettings>()
            .Bind(configuration.GetSection(AuthSecuritySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<WriteLockSettings>()
            .Bind(configuration.GetSection(WriteLockSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
