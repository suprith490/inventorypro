using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InventoryPro.Api.Data;

/// <summary>
/// EF Core database context. Think of this as the JPA EntityManager / Hibernate Session.
/// DbSet&lt;T&gt; properties (added in Phase 4) are the equivalent of JpaRepository&lt;T, ID&gt;.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet<T> is the EF Core equivalent of JpaRepository<T, int>.
    // Each one maps to a table.
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly.
        // This is the equivalent of Hibernate auto-detecting @Entity mappings.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // SQLite has no native decimal type and cannot run SUM/AVG over the TEXT
        // representation EF Core uses by default. The production database is SQL
        // Server (which handles decimal natively); for the SQLite dev fallback we
        // store decimals as REAL so aggregate report queries still work.
        if (Database.IsSqlite())
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(entityType => entityType.GetProperties())
                         .Where(property => property.ClrType == typeof(decimal)))
            {
                property.SetValueConverter(new ValueConverter<decimal, double>(
                    value => (double)value,
                    value => (decimal)value));
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically fills CreatedAt / UpdatedAt for every BaseEntity.
    /// In Spring Boot this is done with @PrePersist and @PreUpdate callbacks
    /// or with JPA auditing (@CreatedDate / @LastModifiedDate).
    /// </summary>
    private void ApplyAuditInformation()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Property(entity => entity.CreatedAt).IsModified = false;
                    break;
            }
        }
    }
}
