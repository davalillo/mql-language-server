using System.Collections.Generic;
using System.Threading;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MqlLanguageServer.Analysis;

/// <summary>
/// Contract for a single semantic diagnostic rule (issue #28).
/// Rules are pure functions of the rule context: no static mutable state,
/// each rule independently unit-testable.
/// </summary>
public interface ISemanticRule
{
    /// <summary>
    /// Run the rule against the analysis context and return any diagnostics.
    /// Must honor <see cref="SemanticRuleContext.Token"/> via
    /// <see cref="CancellationToken.ThrowIfCancellationRequested"/> at reasonable intervals.
    /// </summary>
    IEnumerable<Diagnostic> Check(SemanticRuleContext context);
}

/// <summary>
/// Immutable input passed to every <see cref="ISemanticRule"/>.
/// </summary>
/// <param name="File">Parsed symbol model, or null when only raw content is available.</param>
/// <param name="Content">Raw document text (never null or empty when handed to rules).</param>
/// <param name="Language">Target language variant.</param>
/// <param name="BaseCode">Numeric diagnostic code base (1000 for MQL4, 5000 for MQL5).</param>
/// <param name="Token">Cancellation token linked to the diagnostic request timeout.</param>
public record SemanticRuleContext(
    MqlFile? File,
    string Content,
    MqlLanguage Language,
    int BaseCode,
    CancellationToken Token);