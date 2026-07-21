using System.Collections.Generic;

namespace MqlLanguageServer.Mql4.Builtins;

/// <summary>
/// Abstraction over MQL4 and MQL5 built-in function/variable registries.
/// </summary>
public interface IMqlBuiltins
{
    /// <summary>
    /// Built-in function signatures keyed by function name.
    /// </summary>
    Dictionary<string, string> BuiltInFunctions { get; }

    /// <summary>
    /// Built-in variable descriptions keyed by variable name.
    /// </summary>
    Dictionary<string, string> BuiltInVariables { get; }

    /// <summary>
    /// Check if a name is a built-in function.
    /// </summary>
    bool IsBuiltinFunction(string name);

    /// <summary>
    /// Check if a name is a built-in variable.
    /// </summary>
    bool IsBuiltinVariable(string name);

    /// <summary>
    /// Check if a name is a built-in function or variable.
    /// </summary>
    bool IsBuiltin(string name);

    /// <summary>
    /// Get the signature for a built-in function, or null if not found.
    /// </summary>
    string? GetBuiltinFunctionSignature(string name);

    /// <summary>
    /// Get the description for a built-in variable, or null if not found.
    /// </summary>
    string? GetBuiltinVariableDescription(string name);
}
