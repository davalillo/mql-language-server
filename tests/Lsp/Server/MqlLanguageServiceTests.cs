using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

public class MqlLanguageServiceTests
{
    [Fact]
    public void GetParser_Mql4_Returns_Mql4AntlrParser_Instance()
    {
        var service = new MqlLanguageService(new Mql4AntlrParser());
        var parser = service.GetParser(MqlLanguage.Mql4);

        Assert.NotNull(parser);
        Assert.IsType<Mql4AntlrParser>(parser);
    }

    [Fact]
    public void GetParser_Mql5_Returns_Null_Without_Throwing()
    {
        var service = new MqlLanguageService(new Mql4AntlrParser());
        var parser = service.GetParser(MqlLanguage.Mql5);

        Assert.Null(parser);
    }
}
