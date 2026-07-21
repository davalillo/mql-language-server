using System;
using System.Collections.Generic;

namespace MqlLanguageServer.Mql4.Builtins;

/// <summary>
/// Instance adapter that exposes the static Mql4Builtins registry through IMqlBuiltins.
/// F7 fix: static classes cannot implement interfaces; the static API is preserved unchanged.
/// </summary>
public class Mql4BuiltinsAdapter : IMqlBuiltins
{
    /// <inheritdoc/>
    public Dictionary<string, string> BuiltInFunctions => Mql4Builtins.BuiltInFunctions;

    /// <inheritdoc/>
    public Dictionary<string, string> BuiltInVariables => Mql4Builtins.BuiltInVariables;

    /// <inheritdoc/>
    public bool IsBuiltinFunction(string name) => Mql4Builtins.IsBuiltinFunction(name);

    /// <inheritdoc/>
    public bool IsBuiltinVariable(string name) => Mql4Builtins.IsBuiltinVariable(name);

    /// <inheritdoc/>
    public bool IsBuiltin(string name) => Mql4Builtins.IsBuiltin(name);

    /// <inheritdoc/>
    public string? GetBuiltinFunctionSignature(string name) => Mql4Builtins.GetBuiltinFunctionSignature(name);

    /// <inheritdoc/>
    public string? GetBuiltinVariableDescription(string name) => Mql4Builtins.GetBuiltinVariableDescription(name);
}
