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

    /// <summary>
    /// Issue #55 regression: a cursor on a USE of a function-local variable must
    /// hover the local (via the FindSymbolDefinition refinement), not the
    /// containing function. Companion: the cursor on the function's NAME still
    /// hovers the function.
    /// </summary>
    [Fact]
    public async Task HoverHandler_LocalUse_HoversLocal_NotContainingFunctionAsync()
    {
        // Arrange - a function with a distinctly named local variable
        var content = "void OnTick()\n{\n    int innerTicks = 0;\n    innerTicks = innerTicks + 1;\n}\n";
        var testFilePath = Path.Combine(Path.GetTempPath(), "TestHoverLocal.mq4");
        File.WriteAllText(testFilePath, content);

        try
        {
            var documentStore = new OpenDocumentStore();
            var handler = new HoverHandler(MockLogger<HoverHandler>(), new Mql4AntlrParser(), documentStore);

            // Cursor on the innerTicks use inside the body (line 3).
            var useRequest = new HoverParams
            {
                TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(testFilePath)),
                Position = new Position(3, 5)
            };

            var useResult = await handler.Handle(useRequest, CancellationToken.None);

            // Assert - hover shows the local variable, not the containing OnTick.
            Assert.NotNull(useResult);
            var useContent = useResult!.Contents.MarkupContent!.Value;
            Assert.Contains("## innerTicks", useContent);
            Assert.Contains("**Kind:** Variable", useContent);
            Assert.DoesNotContain("## OnTick", useContent);

            // Companion: cursor on the function's NAME still hovers OnTick.
            var nameRequest = new HoverParams
            {
                TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(testFilePath)),
                Position = new Position(0, 5)
            };

            var nameResult = await handler.Handle(nameRequest, CancellationToken.None);

            Assert.NotNull(nameResult);
            var nameContent = nameResult!.Contents.MarkupContent!.Value;
            Assert.Contains("## OnTick", nameContent);
        }
        finally
        {
            if (File.Exists(testFilePath)) File.Delete(testFilePath);
        }
    }
}