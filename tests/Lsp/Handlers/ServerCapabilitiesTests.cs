// using Xunit;
// using OmniSharp.Extensions.LanguageServer.Protocol;
// using OmniSharp.Extensions.LanguageServer.Protocol.Document;
// using OmniSharp.Extensions.LanguageServer.Protocol.Models;
// using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
// using OmniSharp.Extensions.LanguageServer.Server;
// using OmniSharp.Extensions.LanguageServer.Protocol.Server;
// using MqlLanguageServer.Lsp.Handlers;
// using MqlLanguageServer.Lsp.Server;
// using MqlLanguageServer.Models;
// using MqlLanguageServer.Parser;
// using Microsoft.Extensions.Logging;
// using Moq;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;

// namespace MqlLanguageServer.Tests.Lsp.Handlers;

// /// <summary>
// /// Tests for LSP Server capabilities publication
// /// Verifies that textDocumentSync and provider capabilities are correctly advertised
// /// </summary>
// public class ServerCapabilitiesTests
// {
//     /// <summary>
//     /// Test that handlers have proper GetRegistrationOptions implementation
//     /// </summary>
//     [Fact]
//     public void CompletionHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<CompletionHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var handler = new CompletionHandler(loggerMock.Object, parserMock.Object, documentStore);

//         var capability = new CompletionCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that DefinitionHandler advertises definitionProvider capability
//     /// </summary>
//     [Fact]
//     public void DefinitionHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<DefinitionHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var globalSymbolIndex = new GlobalSymbolIndex();
//         var handler = new DefinitionHandler(loggerMock.Object, parserMock.Object, documentStore, globalSymbolIndex);

//         var capability = new DefinitionCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that HoverHandler advertises hoverProvider capability
//     /// </summary>
//     [Fact]
//     public void HoverHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<HoverHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var handler = new HoverHandler(loggerMock.Object, parserMock.Object, documentStore);

//         var capability = new HoverCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that ReferencesHandler advertises referencesProvider capability
//     /// </summary>
//     [Fact]
//     public void ReferencesHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<ReferencesHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var globalSymbolIndex = new GlobalSymbolIndex();
//         var handler = new ReferencesHandler(loggerMock.Object, parserMock.Object, documentStore, globalSymbolIndex);

//         var capability = new ReferenceCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that DidOpenTextDocumentHandler has proper sync configuration
//     /// </summary>
//     [Fact]
//     public void DidOpenTextDocumentHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<DidOpenTextDocumentHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var globalSymbolIndex = new GlobalSymbolIndex();
//         var handler = new DidOpenTextDocumentHandler(loggerMock.Object, parserMock.Object, documentStore, globalSymbolIndex);

//         var capability = new TextSynchronizationCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//         // Verify MQL4 file patterns are registered
//         var patterns = options.DocumentSelector.Select(f => f.Pattern).ToList();
//         Assert.Contains("**/*.mq4", patterns);
//         Assert.Contains("**/*.mqh", patterns);
//     }

//     /// <summary>
//     /// Test that DidChangeTextDocumentHandler has proper sync configuration
//     /// </summary>
//     [Fact]
//     public void DidChangeTextDocumentHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<DidChangeTextDocumentHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var handler = new DidChangeTextDocumentHandler(loggerMock.Object, parserMock.Object, documentStore);

//         var capability = new TextSynchronizationCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that DocumentSymbolHandler advertises documentSymbolProvider capability
//     /// </summary>
//     [Fact]
//     public void DocumentSymbolHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<DocumentSymbolHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var handler = new DocumentSymbolHandler(loggerMock.Object, parserMock.Object, documentStore);

//         var capability = new DocumentSymbolCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that RenameHandler advertises renameProvider capability
//     /// </summary>
//     [Fact]
//     public void RenameHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<RenameHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var globalSymbolIndex = new GlobalSymbolIndex();
//         var handler = new RenameHandler(loggerMock.Object, parserMock.Object, documentStore, globalSymbolIndex);

//         var capability = new RenameCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that CodeActionHandler advertises codeActionProvider capability
//     /// </summary>
//     [Fact]
//     public void CodeActionHandler_GetRegistrationOptions_ReturnsValidOptions()
//     {
//         // Arrange
//         var loggerMock = new Mock<ILogger<CodeActionHandler>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var handler = new CodeActionHandler(loggerMock.Object, parserMock.Object, documentStore);

//         var capability = new CodeActionCapability();
//         var clientCapabilities = new ClientCapabilities();

//         // Act
//         var options = handler.GetRegistrationOptions(capability, clientCapabilities);

//         // Assert
//         Assert.NotNull(options);
//         Assert.NotNull(options.DocumentSelector);
//         Assert.NotEmpty(options.DocumentSelector);
//     }

//     /// <summary>
//     /// Test that all handlers register MQL4 file patterns correctly
//     /// </summary>
//     [Theory]
//     [InlineData(typeof(CompletionHandler))]
//     [InlineData(typeof(DefinitionHandler))]
//     [InlineData(typeof(HoverHandler))]
//     [InlineData(typeof(ReferencesHandler))]
//     [InlineData(typeof(DocumentSymbolHandler))]
//     [InlineData(typeof(RenameHandler))]
//     [InlineData(typeof(CodeActionHandler))]
//     [InlineData(typeof(DidOpenTextDocumentHandler))]
//     [InlineData(typeof(DidChangeTextDocumentHandler))]
//     [InlineData(typeof(DeclarationHandler))]
//     public void Handler_RegistersMql4FilePatterns(Type handlerType)
//     {
//         // This test verifies that all handlers register for MQL4 file patterns
//         // by checking that GetRegistrationOptions returns valid DocumentSelector

//         // Create handler using reflection (since we need different constructors)
//         var loggerMock = new Mock<ILogger<object>>();
//         var parserMock = new Mock<Mql4AntlrParser>();
//         var documentStore = new OpenDocumentStore();
//         var globalSymbolIndex = new GlobalSymbolIndex();

//         object handler;
//         try
//         {
//             handler = Activator.CreateInstance(handlerType,
//                 loggerMock.Object, parserMock.Object, documentStore, globalSymbolIndex);
//         }
//         catch
//         {
//             try
//             {
//                 handler = Activator.CreateInstance(handlerType,
//                     loggerMock.Object, parserMock.Object, documentStore);
//             }
//             catch
//             {
//                 handler = Activator.CreateInstance(handlerType,
//                     loggerMock.Object, parserMock.Object);
//             }
//         }

//         Assert.NotNull(handler);

//         // Get the GetRegistrationOptions method
//         var method = handlerType.GetMethod("GetRegistrationOptions");
//         Assert.NotNull(method);

//         // Invoke the method
//         var capabilityType = method.GetParameters()[0].ParameterType;
//         var clientCapabilitiesType = method.GetParameters()[1].ParameterType;

//         var capability = Activator.CreateInstance(capabilityType);
//         var clientCapabilities = Activator.CreateInstance(clientCapabilitiesType);

//         var options = method.Invoke(handler, new[] { capability, clientCapabilities });

//         // Assert
//         Assert.NotNull(options);
//         var documentSelectorProperty = options.GetType().GetProperty("DocumentSelector");
//         Assert.NotNull(documentSelectorProperty);

//         var documentSelector = documentSelectorProperty.GetValue(options) as IEnumerable<TextDocumentFilter>;
//         Assert.NotNull(documentSelector);
//         Assert.NotEmpty(documentSelector);
//     }
// }
