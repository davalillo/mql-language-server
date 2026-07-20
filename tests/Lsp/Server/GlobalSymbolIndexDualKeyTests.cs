using System.Collections.Generic;
using System.Linq;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Lsp.Server;

/// <summary>
/// Shares the GlobalSymbolIndex singleton with the rest of the GlobalSymbolIndex Tests collection
/// to prevent cross-test contamination (spec REQ-TD-05). The collection fixture clears the index
/// before and after the collection runs; tests must not call Clear() themselves.
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class GlobalSymbolIndexDualKeyTests
{
    [Fact]
    public void CrossLanguage_Query_Does_Not_Leak()
    {
        var mql4Symbol = new MqlSymbol { Name = "OrderSend", FilePath = "/x.mq4" };
        var mql5Symbol = new MqlSymbol { Name = "OrderSend", FilePath = "/x.mq5" };

        GlobalSymbolIndex.Instance.AddFile("/x.mq4", MqlLanguage.Mql4, new List<MqlSymbol> { mql4Symbol });
        GlobalSymbolIndex.Instance.AddFile("/x.mq5", MqlLanguage.Mql5, new List<MqlSymbol> { mql5Symbol });

        var mql5Results = GlobalSymbolIndex.Instance.FindSymbol("OrderSend", MqlLanguage.Mql5);
        Assert.Single(mql5Results);
        Assert.Equal(MqlLanguage.Mql5, mql5Results[0].Language);

        var mql4Results = GlobalSymbolIndex.Instance.FindSymbol("OrderSend", MqlLanguage.Mql4);
        Assert.Single(mql4Results);
        Assert.Equal(MqlLanguage.Mql4, mql4Results[0].Language);
    }

    [Fact]
    public void FindSymbol_Name_Overload_Returns_Both_Languages()
    {
        var mql4Symbol = new MqlSymbol { Name = "OrderSend", FilePath = "/x.mq4" };
        var mql5Symbol = new MqlSymbol { Name = "OrderSend", FilePath = "/x.mq5" };

        GlobalSymbolIndex.Instance.AddFile("/x.mq4", MqlLanguage.Mql4, new List<MqlSymbol> { mql4Symbol });
        GlobalSymbolIndex.Instance.AddFile("/x.mq5", MqlLanguage.Mql5, new List<MqlSymbol> { mql5Symbol });

        var results = GlobalSymbolIndex.Instance.FindSymbol("OrderSend");
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Language == MqlLanguage.Mql4);
        Assert.Contains(results, r => r.Language == MqlLanguage.Mql5);
    }
}
