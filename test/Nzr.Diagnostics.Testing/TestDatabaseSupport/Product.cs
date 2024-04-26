
namespace Nzr.Diagnostics.Testing.TestDatabaseSupport;

/// <summary>
/// Represents a product with a unique identifier, name, and price.
/// </summary>
/// <param name="Id">The unique identifier for the product.</param>
/// <param name="Name">The name of the product.</param>
/// <param name="Price">The price of the product.</param>
public record Product(long Id, string Name, decimal Price);
