# Nzr.Diagnostics

![GitHub last commit](https://img.shields.io/github/last-commit/marionzr/nzr.diagnostics)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/marionzr/nzr.diagnostics/build-test-and-publish.yml)
![GitHub License](https://img.shields.io/github/license/marionzr/nzr.diagnostics)

Nzr.Diagnostics is a collection of diagnostic utilities and tools, structured as multiple inner projects distributed as NuGet packages. These tools are designed to help with diagnostics, logging, and debugging in your .NET applications.

## Projects

Feel free to explore the individual projects and their respective README files for more detailed information.

### Nzr.Diagnostics.OperationTagGenerator

A project to generate operation tags for tracking and debugging purposes.
[README](src/Nzr.Diagnostics.OperationTagGenerator/README.md)

![NuGet Version](https://img.shields.io/nuget/v/Nzr.Diagnostics.OperationTagGenerator)
![NuGet Downloads](https://img.shields.io/nuget/dt/Nzr.Diagnostics.OperationTagGenerator)

## Nzr.Diagnostics.HealthChecks

A collection of HealthChecks for reporting the health of app infrastructure components.
[README](src/Nzr.Diagnostics.HealthChecks/README.md)

![NuGet Version](https://img.shields.io/nuget/v/Nzr.Diagnostics.HealthChecks)
![NuGet Downloads](https://img.shields.io/nuget/dt/Nzr.Diagnostics.HealthChecks)

## How to Contribute

We welcome contributions to the Nzr.Diagnostics project! Here's how you can get started:

### Steps to Contribute:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them with a descriptive message.
4. Push your changes to your fork and create a pull request.

To ensure consistency in commit messages across all contributions, we require the use of a commit message hook. Please follow the instructions below to set it up:

## Commit Message Hook Configuration

To maintain consistent and structured commit messages, configure the Git commit message hook as follows:

### Windows (PowerShell)

```powershell
cp .\commit-msg .\.git\hooks\commit-msg
```

### Linux/Mac (Bash)

```bash
cp commit-msg .git/hooks/commit-msg
```

This hook ensures that all commit messages follow the required format:

```text
<type>(<scope>): <title>

<Description with bullet points>

<Tags>: The name of modules or topics involved
```

### Example Commit Message

```text
feat(OperationTagGenerator): add TagGenerator 

Add a TagGenerator class to generate operation tags for tracking and debugging purposes.

Tags: Tag, Debug, Diagnostics
```

---

## License

Nzr.Diagnostics and all its projects are licensed under the Apache License, Version 2.0, January 2004. You may obtain a copy of the License at:

```
http://www.apache.org/licenses/LICENSE-2.0
```

# Disclaimer

This project is provided "as-is" without any warranty or guarantee of its functionality. The author assumes no responsibility or liability for any issues, damages, or consequences arising from the use of this code, whether direct or indirect. By using this project, you agree that you are solely responsible for any risks associated with its use, and you will not hold the author accountable for any loss, injury, or legal ramifications that may occur.

Please ensure that you understand the code and test it thoroughly before using it in any production environment.
