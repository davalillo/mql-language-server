using System;
using System.Collections.Generic;
using System.Threading;
using MqlLanguageServer.Analysis.Rules;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Analysis;

/// <summary>
/// MQL-native semantic analyzer (issue #28).
///
/// Runs a fixed set of <see cref="ISemanticRule"/> checks that are cheap and
/// conservative: no type inference, no cross-file resolution, no initializer
/// semantics beyond simple line-scan shapes. Designed to fit inside the
/// diagnostics request timeout (2s linked CTS in DiagnosticHandler).
/// </summary>
public sealed class SemanticAnalyzer
{
    private readonly IMqlBuiltins[]? _builtins;

    /// <summary>
    /// Builtins are reserved for future phases (e.g. MQL4-only API misuse via
    /// builtin API tables). Phase 1 rules do not use them; the parameter exists
    /// so the analyzer is composition-ready without a breaking ctor change.
    /// </summary>
    public SemanticAnalyzer(IMqlBuiltins[]? builtins = null)
    {
        _builtins = builtins;
    }

    /// <summary>
    /// Ordered rule set. New rules append to the end to keep emitted
    /// diagnostic ordering stable between runs.
    /// </summary>
    private static List<ISemanticRule> CreateDefaultRules() => new()
    {
        new InputModifierRule(),
        new PropertyDirectiveRule(),
        new LanguageMisuseRule(),
        new Mql4OnlyApiRule(),
        new ConversionRule()
    };

    /// <summary>
    /// Run all semantic rules and return their diagnostics (possibly empty).
    /// Never throws except on cancellation.
    /// </summary>
    public List<Diagnostic> Analyze(MqlFile? file, string content, MqlLanguage language, CancellationToken token)
    {
        var diagnostics = new List<Diagnostic>();

        if (string.IsNullOrEmpty(content) || token.IsCancellationRequested)
        {
            return diagnostics;
        }

        var baseCode = language == MqlLanguage.Mql5 ? DiagnosticCodes.Mql5Base : DiagnosticCodes.Mql4Base;
        var context = new SemanticRuleContext(file, content, language, baseCode, token);

        foreach (var rule in CreateDefaultRules())
        {
            token.ThrowIfCancellationRequested();

            try
            {
                diagnostics.AddRange(rule.Check(context));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // A defective rule must never take down the diagnostics request:
                // skip the rule and keep the remaining analysis running. Logged
                // by the caller if needed; semantic analysis is best-effort.
            }
        }

        return diagnostics;
    }
}

/// <summary>
/// Numeric diagnostic-code bases shared with DiagnosticHandler.
/// Kept here so semantic rules do not reference the LSP handler class.
/// </summary>
internal static class DiagnosticCodes
{
    public const int Mql4Base = 1000;
    public const int Mql5Base = 5000;
}