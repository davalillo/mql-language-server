using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// Tests for HoverHandler — verifies the MarkupContent response shape for MQL4.
/// The handler was switched from MarkedString to MarkupContent during the
/// add-mql5-support change (Slice 3). This test locks the new wire format
/// for MQL4 clients (S-006 follow-up).
/// </summary>
public class HoverHandlerTests
{
    private static ILogger<T> MockLogger<T>() where T : class => Substitute.For<ILogger<T>>();

    [Fact]
    public async Task HoverHandler_Mql4_Returns_MarkupContent_ShapeAsync()
    {
        // Arrange - MQL4 code using a builtin with a known signature and example
        var content = "void OnTick()\n{\n    OrderSend(Symbol(), OP_BUY, 1.0, Ask, 3, 0, 0, \"c\", 0, 0, clrBlue);\n}\n";
        var testFilePath = Path.Combine(Path.GetTempPath(), "TestHoverMql4.mq4");
        File.WriteAllText(testFilePath, content);

        try
        {
            var documentStore = new OpenDocumentStore();
            var handler = new HoverHandler(MockLogger<HoverHandler>(), new Mql4AntlrParser(), documentStore);

            var request = new HoverParams
            {
                TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(testFilePath)),
                Position = new Position(2, 5) // on OrderSend
            };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert - the response must use MarkupContent (not MarkedString)
            Assert.NotNull(result);
            var hover = Assert.IsType<Hover>(result);

            // S-006: verify MarkupContent shape, not MarkedStrings
            Assert.NotNull(hover.Contents);
            Assert.NotNull(hover.Contents.MarkupContent);
            Assert.Equal(MarkupKind.Markdown, hover.Contents.MarkupContent!.Kind);

            // Content should include the builtin name and MQL4 context
            var contentString = hover.Contents.MarkupContent!.Value;
            Assert.Contains("OrderSend", contentString);
            Assert.Contains("MQL4 Built-in", contentString);

            // The markdown fence should use the mql4 language tag
            Assert.Contains("```mql4", contentString);
        }
        finally
        {
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }
}