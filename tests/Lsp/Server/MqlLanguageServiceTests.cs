using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

public class MqlLanguageServiceTests
{
    [Fact]
    public void ResolveParser_Mql4_Returns_Mql4AntlrParser_Instance()
    {
        var service = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        var parser = service.ResolveParser(MqlLanguage.Mql4);

        Assert.NotNull(parser);
        Assert.IsType<Mql4AntlrParser>(parser);
    }

    [Fact]
    public void ResolveParser_Mql5_Returns_Mql5AntlrParser_Instance()
    {
        var service = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        var parser = service.ResolveParser(MqlLanguage.Mql5);

        Assert.NotNull(parser);
        Assert.IsType<Mql5AntlrParser>(parser);
    }

    [Theory]
    [InlineData(MqlLanguage.Mql4)]
    [InlineData((MqlLanguage)999)] // unknown/invalid language defaults to MQL4
    public void ResolveParser_NonMql5_Returns_Mql4AntlrParser_Instance(MqlLanguage language)
    {
        // REQ-PA-03: unknown language defaults to MQL4 parser.
        // ResolveParser only branches on Mql5; everything else falls through to Mql4.
        var service = new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser());
        var parser = service.ResolveParser(language);

        Assert.NotNull(parser);
        Assert.IsType<Mql4AntlrParser>(parser);
    }
}
