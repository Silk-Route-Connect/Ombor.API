using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PartnerBalanceConfiguration : IEntityTypeConfiguration<PartnerBalance>
{
    public void Configure(EntityTypeBuilder<PartnerBalance> builder)
    {
        builder.ToView("View_PartnerBalance");

        builder.HasNoKey();

        builder
            .Property(v => v.PartnerAdvance)
            .HasCurrencyPrecision();
        builder
            .Property(v => v.CompanyAdvance)
            .HasCurrencyPrecision();
        builder
            .Property(v => v.PayableDebt)
            .HasCurrencyPrecision();
        builder
            .Property(v => v.ReceivableDebt)
            .HasCurrencyPrecision();
    }
}
