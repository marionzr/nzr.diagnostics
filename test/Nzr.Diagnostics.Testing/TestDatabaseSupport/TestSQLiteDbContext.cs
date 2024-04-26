using Microsoft.EntityFrameworkCore;

namespace Nzr.Diagnostics.Testing.TestDatabaseSupport;

/// <summary>
/// Represents the database context for interacting with a SQLite database in a testing environment.
/// Inherits from <see cref="DbContext"/> and defines the <see cref="Products"/> DbSet.
/// </summary>
public class TestSQLiteDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestSQLiteDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public TestSQLiteDbContext(DbContextOptions<TestSQLiteDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{Product}"/> representing the collection of products.
    /// </summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>
    /// Configures the model for the database context, including seeding data for the <see cref="Products"/> table.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasData(
                new Product(1, "Product 1", 19.99m),
                new Product(2, "Product 2", 9.99m)
            );
    }
}
