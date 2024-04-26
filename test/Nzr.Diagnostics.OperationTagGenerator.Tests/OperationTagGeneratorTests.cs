using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nzr.Diagnostics.Testing.TestDatabaseSupport;
using Snapshooter.Xunit;

namespace Nzr.Diagnostics.OperationTagGenerator.Tests;

[Collection(TestSQLiteDbContextCollection.CollectionName)]
public class OperationTagGeneratorTests : IAsyncLifetime
{

    private readonly TestSQLiteDbContextFixture _fixture;

    public OperationTagGeneratorTests(TestSQLiteDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public void NewTag_ShouldGenerateValidOperationTag()
    {
        // Act

        var operationTag = TagGenerator.NewTag();

        // Assert

        operationTag.Should().MatchSnapshot();
    }

    [Fact]
    public void NewTag_Should_Be_Included_In_Linq_GeneratedSql()
    {
        // Arrange

        using var context = _fixture.GetDbContext();

        // Act

        var query = context.Products
            .Where(p => p.Price > 10)
            .TagWith(TagGenerator.NewTag());

        var sql = query.ToQueryString();

        // Assert

        sql.Should().MatchSnapshot();
    }

    [Fact]
    public void NewTag_Should_Be_Included_In_Raw_GeneratedSql()
    {
        // Arrange

        using var context = _fixture.GetDbContext();

        // Act

        var query = context.Products
            .FromSqlRaw("SELECT * FROM Products WHERE Price > 10")
            .TagWith(TagGenerator.NewTag());

        var sql = query.ToQueryString();

        // Assert

        sql.Should().MatchSnapshot();
    }
}
