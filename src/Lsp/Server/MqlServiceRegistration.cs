using Microsoft.Extensions.DependencyInjection;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// The parser pair and the language service that selects between them, wired the
/// way the server resolves them at startup.
/// </summary>
/// <remarks>
/// <para>
/// Issue #169 widened <see cref="MqlLanguageService"/>'s constructor to
/// <see cref="IMqlParser"/> parameters so a test can inject a counting
/// decorator. The container cannot satisfy that signature on its own: the two
/// parameters share one type, and the registered types are the concrete
/// <see cref="Mql4AntlrParser"/> and <see cref="Mql5AntlrParser"/>. Registering
/// only the concrete types (what the server did) left <see cref="IMqlParser"/>
/// unregistered, so DryIoc could not build <see cref="MqlLanguageService"/> and
/// every handler that depends on it became unconstructible — the server exited
/// before answering <c>initialize</c> (v2.7.1).
/// </para>
/// <para>
/// The pair is therefore bound by an explicit factory that names each dialect.
/// Registering <see cref="IMqlParser"/> instead would let the container hand the
/// same parser to both parameters. This helper exists so the registration the
/// server actually uses is what the composition-root test exercises, rather
/// than a copy of it that can drift.
/// </para>
/// </remarks>
public static class MqlServiceRegistration
{
    /// <summary>
    /// Registers the MQL4/MQL5 parsers and the <see cref="MqlLanguageService"/>
    /// that resolves them.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddLanguageServices(this IServiceCollection services)
    {
        services.AddTransient<Mql4AntlrParser>();
        services.AddTransient<Mql5AntlrParser>();
        services.AddSingleton<MqlLanguageService>(provider => new MqlLanguageService(
            provider.GetRequiredService<Mql4AntlrParser>(),
            provider.GetRequiredService<Mql5AntlrParser>()));

        return services;
    }
}
