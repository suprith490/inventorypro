using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.Notes)
            .HasMaxLength(500);

        // Reporting queries filter by product and order by time.
        builder.HasIndex(transaction => new { transaction.ProductId, transaction.CreatedAt });
        builder.HasIndex(transaction => transaction.TransactionType);

        builder.HasOne(transaction => transaction.Product)
            .WithMany(product => product.InventoryTransactions)
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.User)
            .WithMany(user => user.InventoryTransactions)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
