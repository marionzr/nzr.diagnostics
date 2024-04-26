using Nzr.Diagnostics.Testing.TestDatabaseSupport;

namespace Nzr.Diagnostics.OperationTagGenerator.Tests;

/// <summary>
/// Defines a collection for shared test fixtures that involve the <see cref="TestSQLiteDbContextFixture"/>.
/// The collection is used to ensure the test fixture is initialized and disposed of once per test collection.
/// </summary>
[CollectionDefinition(CollectionName)]
public class TestSQLiteDbContextCollection : ICollectionFixture<TestSQLiteDbContextFixture>
{
    /// <summary>
    /// The name of the test collection.
    /// Used to associate tests with the collection fixture.
    /// </summary>
    public const string CollectionName = "TestSQLiteDbContextCollection";
}
