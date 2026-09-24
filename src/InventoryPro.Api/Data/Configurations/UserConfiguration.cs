using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        // A unique index guarantees no two accounts share an email.
        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasMany(user => user.Purchases)
            .WithOne(purchase => purchase.User)
            .HasForeignKey(purchase => purchase.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.Sales)
            .WithOne(sale => sale.User)
            .HasForeignKey(sale => sale.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.InventoryTransactions)
            .WithOne(transaction => transaction.User)
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
