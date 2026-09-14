using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MqlLanguageServer.Lsp.Handlers;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Handlers;

/// <summary>
/// REQ-CP-10 / REQ-HD-09: color handlers registered, capability auto-advertised,
/// selectors satisfy the common selector rule, prior capabilities unchanged.
/// </summary>
public class ColorRegistrationTests
{
    // REQ-HD-09 scenario 2 — selectors contain **/*.mq5 + language id mql5

    [Fact]
    public void DocumentColorHandler_Selector_Contains_Mq5_Pattern_And_Mql5_Language()
    {
        var handler = new DocumentColorHandler(
            Substitute.For<ILogger<DocumentColorHandler>>(),
            new MqlLanguageService(new Mql4AntlrParser(), new Mql5AntlrParser()),
            new OpenDocumentStore(),
            new IMqlBuiltins[] { new Mql4BuiltinsAdapter() });

        var options = handler.GetRegistrationOptions(new ColorProviderCapability(), new ClientCapabilities());

        Assert.NotNull(options);
        var patterns = options.DocumentSelector!.Select(f => f.Pattern).Where(p => p != null).ToList();
        var languages = options.DocumentSelector!.Select(f => f.Language).Where(l => l != null).ToList();
        Assert.Contains("**/*.mq5", patterns);
        Assert.Contains("mql5", languages);
    }

    // D4 — IColorPresentationHandler does NOT participate in registration:
    // it must have no GetRegistrationOptions method at all.

    [Fact]
    public void ColorPresentationHandler_Has_No_Registration_Options()
    {
        var method = typeof(ColorPresentationHandler).GetMethod(
            "GetRegistrationOptions", BindingFlags.Public | BindingFlags.Instance);

        Assert.Null(method);
        Assert.True(typeof(OmniSharp.Extensions.LanguageServer.Protocol.IDoesNotParticipateInRegistration)
            .IsAssignableFrom(typeof(OmniSharp.Extensions.LanguageServer.Protocol.Document.IColorPresentationHandler)));
    }

    // REQ-HD-09 scenario 1 — both handlers registered exactly once in Program.cs
    // (composition-root source scan; Program is not public so the chain is text).

    [Fact]
    public void Program_Registers_Both_Color_Handlers_Exactly_Once()
    {
        var programSource = ReadProgramSource();

        Assert.Equal(1, CountOccurrences(programSource, ".WithHandler<DocumentColorHandler>()"));
        Assert.Equal(1, CountOccurrences(programSource, ".WithHandler<ColorPresentationHandler>()"));
    }

    // REQ-CP-10 / REQ-HD-09 — colorProvider derives ONLY from DocumentColorHandler
    // registration: DocumentColorHandler implements IDocumentColorHandler
    // (IRegistration<DocumentColorRegistrationOptions, ColorProviderCapability>),
    // so the library's converter scan (RegistrationOptionsKey("ColorProvider"))
    // advertises the capability with no manual MqlServerCapabilities edits.

    [Fact]
    public void DocumentColorHandler_Implements_Registration_For_ColorProvider_Capability()
    {
        Assert.True(typeof(OmniSharp.Extensions.LanguageServer.Protocol.Document.IDocumentColorHandler)
            .IsAssignableFrom(typeof(DocumentColorHandler)));

        var registrationType = typeof(DocumentColorHandler).GetInterfaces()
            .Single(i => i.IsGenericType && i.Name.StartsWith("IRegistration`"));
        Assert.Equal(typeof(ColorProviderCapability), registrationType.GetGenericArguments()[1]);
    }

    // REQ-HD-09 scenario 4 — previously registered handlers keep their chain
    // (spot-check the composition root still lists every pre-change handler).

    [Fact]
    public void Program_Prior_Handler_Registrations_Unchanged()
    {
        var programSource = ReadProgramSource();

        foreach (var handler in new[]
                 {
                     "DocumentSymbolHandler", "DefinitionHandler", "ReferencesHandler",
                     "CompletionHandler", "HoverHandler", "RenameHandler", "DiagnosticHandler",
                     "DidOpenTextDocumentHandler", "DidChangeTextDocumentHandler"
                 })
        {
            Assert.Contains($".WithHandler<{handler}>()", programSource);
        }
    }

    private static string ReadProgramSource()
    {
        // Test assembly lives at <repo>/tests/bin/<tfm>/<cfg>/ — Program.cs is
        // 4 directories up under src/.
        var here = Path.GetDirectoryName(typeof(ColorRegistrationTests).Assembly.Location)!;
        var path = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "..", "src", "Program.cs"));
        Assert.True(File.Exists(path), $"Program.cs not found: {path}");
        return File.ReadAllText(path);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}