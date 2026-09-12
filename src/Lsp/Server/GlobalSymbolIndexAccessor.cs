using System;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Injectable accessor for the global symbol index (issue #25c).
///
/// <para>Before this class, handlers reached the shared index through the
/// <see cref="GlobalSymbolIndex.Instance"/> static singleton directly (13 call
/// sites) — hidden global state that made handler unit isolation and test
/// seam replacement impossible, even though the backward-compatible test
/// constructors already accepted (and silently ignored) a
/// <c>GlobalSymbolIndex</c> parameter.</para>
///
/// <para><b>Bounded DI design</b>: OmniSharp's <c>.WithHandler&lt;T&gt;</c>
/// resolves handlers from the service container, and the production
/// composition root (<c>Program.cs</c>) already registers
/// <c>services.AddSingleton&lt;GlobalSymbolIndex&gt;()</c>. This accessor is
/// registered against that instance, and handlers receive it through the
/// <c>LanguageAwareHandlerBase</c>
/// constructor chain. A parameterless constructor keeps the accessor usable
/// from contexts that are not DI-constructed (it falls back to
/// <see cref="GlobalSymbolIndex.Instance"/>, the pre-existing singleton
/// behavior), so the singleton itself stays intact and the migration is
/// behavior-preserving.</para>
/// </summary>
public sealed class GlobalSymbolIndexAccessor
{
    private readonly Func<GlobalSymbolIndex> _resolver;

    /// <summary>
    /// DI constructor: resolves the given instance (the composition root
    /// registers a <see cref="GlobalSymbolIndex"/> singleton).
    /// </summary>
    public GlobalSymbolIndexAccessor(GlobalSymbolIndex index)
    {
        _resolver = () => index ?? throw new InvalidOperationException(
            "GlobalSymbolIndexAccessor was constructed with a null index.");
    }

    /// <summary>
    /// Non-DI constructor: falls back to the static
    /// <see cref="GlobalSymbolIndex.Instance"/> singleton (the pre-existing
    /// behavior, preserved for parser-internal and legacy paths).
    /// </summary>
    public GlobalSymbolIndexAccessor()
        : this(GlobalSymbolIndex.Instance)
    {
    }

    /// <summary>
    /// The shared symbol index. Handlers must never cache this beyond a
    /// single request: tests replace the accessor (or its index) per fixture.
    /// </summary>
    public GlobalSymbolIndex Index => _resolver();
}