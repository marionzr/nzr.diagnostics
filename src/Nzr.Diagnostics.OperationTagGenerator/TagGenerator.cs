
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nzr.Diagnostics.OperationTagGenerator;

/// <summary>
/// Provides methods to generate operation tags for tracking and debugging purposes.
/// </summary>
public static class TagGenerator
{
    private const string InfoNotAvailable = "(n/a)";
    private static readonly Lazy<AssemblyName?> _initialAssemblyNameLazy = new(GetInitialAssemblyName);
    private static AssemblyName? InitialAssemblyName => _initialAssemblyNameLazy.Value;

    /// <summary>
    /// Determines the initial assembly name by analyzing the call stack.
    /// </summary>
    /// <returns>The initial assembly name.</returns>
    private static AssemblyName? GetInitialAssemblyName()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var stackFrames = new StackTrace().GetFrames();

        if (stackFrames == null)
        {
            return currentAssembly.GetName();
        }

        foreach (var frame in stackFrames)
        {
            var method = frame?.GetMethod();
            var assembly = method?.DeclaringType?.Assembly;

            if (assembly == null || assembly == currentAssembly)
            {
                continue;
            }

            if (Array.Exists(assembly.GetReferencedAssemblies(), refAsm => refAsm.FullName == currentAssembly.FullName))
            {
                return assembly.GetName();
            }
        }

        return currentAssembly.GetName();
    }

    /// <summary>
    /// Generates a new operation tag containing metadata about the caller, which can be used for tracking and debugging.
    /// </summary>
    /// <param name="filePath">The source file path of the caller.</param>
    /// <param name="memberName">The member name of the caller.</param>
    /// <param name="lineNumber">The source line number of the caller.</param>
    /// <returns>A string representing the operation tag.</returns>
    public static string NewTag(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = GetFileName(filePath);

        var sb = new StringBuilder()
            .AppendLine($"Assembly: {InitialAssemblyName?.Name ?? InfoNotAvailable}")
            .AppendLine($"File: {fileName}")
            .AppendLine($"Member: {memberName}")
            .AppendLine($"Line: {lineNumber}");

        return sb.ToString();
    }

    private static string GetFileName(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return fullPath;
        }

        try
        {
            var fileName = Path.GetFileName(fullPath);

            return fileName;
        }
        catch
        {
            return fullPath;
        }
    }
}
