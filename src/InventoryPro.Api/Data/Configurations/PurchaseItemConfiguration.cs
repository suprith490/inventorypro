using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(item => item.PurchaseId);
        builder.HasIndex(item => item.ProductId);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.PurchaseItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
