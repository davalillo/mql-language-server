using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Spec S11 of odd/tasks/preprocessor-branch-diagnostics.md: a name that a file
/// used to declare and no longer declares must leave the workspace NAME index.
/// That index feeds the cross-file suppression of unresolved-symbol diagnostics,
/// so a stale entry silently hides a real 5070 for the rest of the process.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class GlobalSymbolIndexVanishedNameTests
{
    private const string FilePath = "/tmp/index-vanish/branch.mq5";

    private static MqlSymbol Function(string name, int line) => new()
    {
        Name = name,
        Kind = SymbolKind.Function,
        Range = new Range(line, 0, line, 10),
        SelectionRange = new Range(line, 0, line, 10)
    };

    [Fact]
    public void SymbolRemovedFromTheModel_DisappearsFromTheNameIndex()
    {
        var index = GlobalSymbolIndex.Instance;
        try
        {
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol>
            {
                Function("OnStart", 0),
                Function("MISSING_X", 2)
            });

            Assert.NotEmpty(index.FindSymbol("MISSING_X", MqlLanguage.Mql5));
            Assert.NotEmpty(index.FindSymbol("MISSING_X"));

            // The document was re-parsed and no longer declares MISSING_X.
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol> { Function("OnStart", 0) });

            Assert.Empty(index.FindSymbol("MISSING_X", MqlLanguage.Mql5));
            Assert.Empty(index.FindSymbol("MISSING_X"));
            // The surviving declaration is untouched.
            Assert.NotEmpty(index.FindSymbol("OnStart", MqlLanguage.Mql5));
        }
        finally
        {
            index.Clear();
        }
    }

    [Fact]
    public void SymbolStillDeclared_SurvivesTheReplacement()
    {
        var index = GlobalSymbolIndex.Instance;
        try
        {
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol> { Function("KEEP_ME", 0) });
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol> { Function("KEEP_ME", 1) });

            var locations = index.FindSymbol("KEEP_ME", MqlLanguage.Mql5);
            Assert.Single(locations);
            Assert.Equal(1, locations[0].Symbol.Range.Start.Line);
        }
        finally
        {
            index.Clear();
        }
    }

    [Fact]
    public void VanishedNameInAnotherFile_IsNotPruned()
    {
        var index = GlobalSymbolIndex.Instance;
        try
        {
            index.AddFile("/tmp/index-vanish/other.mq5", MqlLanguage.Mql5,
                new List<MqlSymbol> { Function("SHARED_HELPER", 0) });
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol> { Function("SHARED_HELPER", 0) });

            // branch.mq5 stops declaring it; other.mq5 still does.
            index.AddFile(FilePath, MqlLanguage.Mql5, new List<MqlSymbol> { Function("OnStart", 0) });

            var locations = index.FindSymbol("SHARED_HELPER", MqlLanguage.Mql5);
            Assert.Single(locations);
            Assert.Equal("/tmp/index-vanish/other.mq5", locations[0].FilePath);
        }
        finally
        {
            index.Clear();
        }
    }
}
