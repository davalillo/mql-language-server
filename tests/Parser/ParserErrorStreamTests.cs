using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// Issue #21a: the parse-exception catch blocks used to write
/// "Error parsing MQL4/MQL5 file: ..." to STDOUT. On a stdio LSP, stdout is
/// the JSON-RPC transport, so any plain text injected there desynchronizes or
/// crashes the client connection. The message must go to stderr, like the
/// ANTLR error listeners already do.
///
/// The catch is normally unreachable (the #20 guard rejects pathological
/// inputs before ANTLR sees them), so the tests invoke the private core
/// <c>ParseFile(content, filePath, errorListener, cancellationToken)</c> via
/// reflection with a null content string: the first statement
/// (<c>content.TrimStart</c>) throws NRE, the catch runs, and the console
/// routing becomes observable. If the private signature ever changes, the
/// tests fail loudly (missing method) rather than silently passing.
/// </summary>
public class ParserErrorStreamTests
{
    [Fact]
    public void Mql4ParseFile_ExceptionPath_WritesToStderr_NotStdout()
    {
        RunWithCapturedConsole(
            () => InvokePrivateCore(
                typeof(Mql4AntlrParser),
                "MqlLanguageServer.Parser.Mql4AntlrParser",
                new Mql4AntlrParser(),
                content: null,
                filePath: "boom.mq4"),
            out var stdout, out var stderr);

        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Error parsing MQL4 file", stderr.ToString());
    }

    [Fact]
    public void Mql5ParseFile_ExceptionPath_WritesToStderr_NotStdout()
    {
        RunWithCapturedConsole(
            () => InvokePrivateCore(
                typeof(Mql5AntlrParser),
                "MqlLanguageServer.Mql5.Parser.Mql5AntlrParser",
                new Mql5AntlrParser(),
                content: null,
                filePath: "boom.mq5"),
            out var stdout, out var stderr);

        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Error parsing MQL5 file", stderr.ToString());
    }

    private static void RunWithCapturedConsole(
        Action parse,
        out StringWriter stdout,
        out StringWriter stderr)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        stdout = new StringWriter();
        stderr = new StringWriter();

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            parse();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    /// <summary>
    /// Invokes the private core ParseFile overload (4 parameters: content,
    /// filePath, errorListener, cancellationToken) with a null content so its
    /// try body throws and the Console.Error catch executes. The returned
    /// Mql4File/MqlFile is deliberately ignored — the test only observes the
    /// console streams.
    /// </summary>
    private static void InvokePrivateCore(
        Type parserTypeHint,
        string parserTypeName,
        object parser,
        string? content,
        string filePath)
    {
        var type = parserTypeHint.Assembly.GetType(parserTypeName)
            ?? throw new InvalidOperationException($"Parser type not found: {parserTypeName}");

        var core = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m =>
                m.Name == "ParseFile" &&
                m.GetParameters().Length == 4);

        var listenerType = core.GetParameters()[2].ParameterType;
        var listener = Activator.CreateInstance(listenerType, filePath, /* grammar */ parserTypeName.Contains("Mql5") ? "MQL5" : "MQL4");

        core.Invoke(parser, new[] { (object?)content, (object)filePath, listener!, CancellationTokenNone() });
    }

    private static CancellationToken CancellationTokenNone() => CancellationToken.None;
}