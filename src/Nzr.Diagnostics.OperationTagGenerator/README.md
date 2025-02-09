# Nzr.Diagnostics.OperationTagGenerator

![NuGet Version](https://img.shields.io/nuget/v/Nzr.Diagnostics.OperationTagGenerator)
![NuGet Downloads](https://img.shields.io/nuget/dt/Nzr.Diagnostics.OperationTagGenerator)
![GitHub last commit](https://img.shields.io/github/last-commit/marionzr/nzr.diagnostics)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/marionzr/nzr.diagnostics/build-test-and-publish.yml)
![GitHub License](https://img.shields.io/github/license/marionzr/nzr.diagnostics)

`OperationTagGenerator` is a simple utility for generating operation tags that help with 
tracking and debugging method calls. It captures metadata such as the calling assembly, 
source file, method name, and line number.


## Getting Started

### Installation

To install the Nzr.Diagnostics.OperationTagGenerator library, use the NuGet Package Manager:

```
Install-Package Nzr.Diagnostics.OperationTagGenerator
```

or

```bash
dotnet add package Nzr.Diagnostics.OperationTagGenerator
```

### Usage

#### Generating an Operation Tag

You can use `OperationTagGenerator.NewTag()` to generate metadata about where the method is being called from. 
This is useful for logging, debugging, and tracking operations.

##### Example: Using with LINQ Queries

```csharp
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
Nzr.Diagnostics.OperationTagGenerator;

class Program
{
    static void Main()
    {
        using var dbContext = new MyDbContext();
                
        var query = context.Products
            .Where(p => p.Price > 10)
            .TagWith(TagGenerator.NewTag());

        var sql = query.ToQueryString();
        Console.WriteLine(sql);
    }
}
```

Output:
```bash
-- Nzr.Diagnostics.OperationTagGenerator.Demo
-- , File: Program.cs
-- , Member: NewTag_Should_Be_Included_In_Linq_GeneratedSql
-- , Line: 52

SELECT "p"."Id", "p"."Name", "p"."Price"
FROM "Products" AS "p"
WHERE ef_compare("p"."Price", '10.0') > 0
```

##### Example: Logging an Operation

You can also use the operation tag to improve log messages:

```csharp        
var operationTag = OperationTagGenerator.NewTag();
logger.LogInformation($"Processing request. {operationTag}");
```

---

## License
Nzr.Diagnostics.OperationTagGenerator is licensed under the Apache License, Version 2.0, January 2004. You may obtain a copy of the License at:

```
http://www.apache.org/licenses/LICENSE-2.0
```

# Disclaimer

This project is provided "as-is" without any warranty or guarantee of its functionality. The author assumes no responsibility or liability for any issues, damages, or consequences arising from the use of this code, whether direct or indirect. By using this project, you agree that you are solely responsible for any risks associated with its use, and you will not hold the author accountable for any loss, injury, or legal ramifications that may occur.

Please ensure that you understand the code and test it thoroughly before using it in any production environment.
