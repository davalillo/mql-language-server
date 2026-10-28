using Microsoft.Extensions.DependencyInjection;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// The composition root, not the handlers: v2.7.1 shipped binaries that could not
/// start because <see cref="MqlLanguageService"/> took two <c>IMqlParser</c>
/// parameters (the issue #169 seam) while only the concrete parsers were
/// registered, so DryIoc could not build it and the process exited before
/// answering <c>initialize</c>. All 1,813 tests stayed green because every one of
/// them constructs handlers directly and never resolves the graph the server
/// resolves at startup.
/// </summary>
public class MqlServiceRegistrationTests
{
    [Fact]
    public void AddLanguageServices_ResolvesTheLanguageServiceFromItsOwnRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLanguageServices();
        using var provider = services.BuildServiceProvider();

        var languageService = provider.GetRequiredService<MqlLanguageService>();

        Assert.NotNull(languageService);
        Assert.IsType<Mql4AntlrParser>(provider.GetRequiredService<Mql4AntlrParser>());
        Assert.IsType<Mql5AntlrParser>(provider.GetRequiredService<Mql5AntlrParser>());
    }

    [Fact]
    public void AddLanguageServices_ResolvedServiceRoutesEachDialectToItsOwnParser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLanguageServices();
        using var provider = services.BuildServiceProvider();

        var languageService = provider.GetRequiredService<MqlLanguageService>();

        // The pair must stay distinguishable after resolution: a container that
        // satisfied both IMqlParser parameters with one instance would make every
        // MQL5 document parse through the MQL4 grammar.
        Assert.IsType<Mql4AntlrParser>(languageService.ResolveParser(MqlLanguage.Mql4));
        Assert.IsType<Mql5AntlrParser>(languageService.ResolveParser(MqlLanguage.Mql5));
    }
}
