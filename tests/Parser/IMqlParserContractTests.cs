using System.Linq;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

public class IMqlParserContractTests
{
    [Fact]
    public void Mql4AntlrParser_Implements_IMqlParser()
    {
        var parserType = typeof(Mql4AntlrParser);
        var interfaceType = typeof(IMqlParser);

        Assert.Contains(interfaceType, parserType.GetInterfaces());
    }

    [Fact]
    public void Mql4AntlrParser_CanBeAssignedTo_IMqlParser()
    {
        IMqlParser parser = new Mql4AntlrParser();
        Assert.NotNull(parser);
    }
}
