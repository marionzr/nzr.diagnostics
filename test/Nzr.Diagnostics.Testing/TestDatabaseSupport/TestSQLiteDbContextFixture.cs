using Microsoft.EntityFrameworkCore;

namespace Nzr.Diagnostics.Testing.TestDatabaseSupport;

/// <summary>
/// Fixture class for setting up and managing a test SQLite database context.
/// Implements <see cref="IAsyncLifetime"/> for asynchronous setup and cleanup.
/// </summary>
public class TestSQLiteDbContextFixture : IAsyncLifetime
{
    private string _dbPath = null!;
    private readonly Func<DbContextOptions<TestSQLiteDbContext>, TestSQLiteDbContext> _createDbContextFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSQLiteDbContextFixture"/> class.
    /// </summary>
    public TestSQLiteDbContextFixture()
    {
        _createDbContextFunction = (options) => new TestSQLiteDbContext(options);
    }

    /// <summary>
    /// Initializes the database path for the test database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InitializeAsync()
    {
        _dbPath = $"testdb_{Guid.NewGuid()}.db";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the test database by deleting it if it exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the fixture has not been initialized.</exception>
    public async Task ResetDatabaseAsync()
    {
        if (_dbPath == null)
        {
            throw new InvalidOperationException("The Fixtures have not been initialized.");
        }

        using var dbContext = GetDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Gets a new instance of the database context, ensuring the database is created.
    /// </summary>
    /// <returns>A new instance of <see cref="TestSQLiteDbContext"/>.</returns>
    public TestSQLiteDbContext GetDbContext()
    {
        var dbContext = _createDbContextFunction(BuildDbContextOptions());
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    /// <summary>
    /// Builds the <see cref="DbContextOptions{TContext}"/> for the database context.
    /// </summary>
    /// <returns>The configured <see cref="DbContextOptions{TestSQLiteDbContext}"/>.</returns>
    private DbContextOptions<TestSQLiteDbContext> BuildDbContextOptions()
    {
        var options = new DbContextOptionsBuilder<TestSQLiteDbContext>()
           .UseSqlite($"Data Source={_dbPath}")
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine)
           .Options;

        return options;
    }

    /// <summary>
    /// Disposes of the test database by deleting the SQLite file.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisposeAsync()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }
}
