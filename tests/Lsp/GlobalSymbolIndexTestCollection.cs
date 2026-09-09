using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Models;

namespace MqlLanguageServer.Tests.Lsp
{
    /// <summary>
    /// Fixture class that ensures GlobalSymbolIndex is cleaned up after all tests in a test collection
    /// </summary>
    public class GlobalSymbolIndexCollectionFixture : IDisposable
    {
        private bool _disposed = false;

        public GlobalSymbolIndexCollectionFixture()
        {
            // Ensure clean state at the start of each test collection
            GlobalSymbolIndex.Instance.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Cleanup after all tests in the collection have run
                GlobalSymbolIndex.Instance.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Collection definition for tests that use GlobalSymbolIndex
    /// </summary>
    [CollectionDefinition("GlobalSymbolIndex Tests")]
    public class GlobalSymbolIndexTestCollection : ICollectionFixture<GlobalSymbolIndexCollectionFixture>
    {
        // This class has no code, and is never created.
        // Its purpose is to be the place to apply [CollectionDefinition]
    }

    /// <summary>
    /// OCC-03/04/05, REQ-SM-04: occurrence storage in GlobalSymbolIndex.
    /// Name-keyed occurrence lookup, stale-entry removal on re-AddFile,
    /// file-scoped replacement, and definition marking via SelectionRange
    /// overlap. Runs inside the shared GlobalSymbolIndex Tests collection.
    /// </summary>
    [Collection("GlobalSymbolIndex Tests")]
    public class GlobalSymbolIndexOccurrenceTests
    {
        private static List<MqlSymbol> Definition(string name, string filePath)
        {
            // Definition whose SelectionRange covers (line 0, col 4..9).
            return new List<MqlSymbol>
            {
                new MqlSymbol
                {
                    Name = name,
                    FilePath = filePath,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 4),
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 9)),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 4),
                        new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(0, 9))
                }
            };
        }

        private static List<SymbolOccurrence> Occurrences(string name, string filePath, MqlLanguage language, bool isDefinition)
        {
            return new List<SymbolOccurrence>
            {
                new SymbolOccurrence
                {
                    FilePath = filePath,
                    Language = language,
                    Text = name,
                    Line = 0,
                    Column = 4,
                    Length = 5,
                    IsDefinition = isDefinition
                }
            };
        }

        [Fact]
        public void FindOccurrences_ReturnsPositionsByName()
        {
            // OCC-03: occurrences retrievable by name.
            const string path = "/idx/a.mq5";
            GlobalSymbolIndex.Instance.AddFile(
                path, MqlLanguage.Mql5,
                Definition("MySignal", path),
                Occurrences("MySignal", path, MqlLanguage.Mql5, isDefinition: false));

            var results = GlobalSymbolIndex.Instance.FindOccurrences("MySignal");

            Assert.Single(results);
            Assert.Equal(path, results[0].FilePath);
            Assert.Equal(0, results[0].Line);
            Assert.Equal(4, results[0].Column);
            Assert.Equal(5, results[0].Length);
            Assert.Equal(MqlLanguage.Mql5, results[0].Language);
        }

        [Fact]
        public void FindOccurrences_LanguageOverload_FiltersByLanguage()
        {
            const string mq4Path = "/idx/b.mq4";
            const string mq5Path = "/idx/b.mq5";
            GlobalSymbolIndex.Instance.AddFile(
                mq4Path, MqlLanguage.Mql4,
                Definition("Alpha", mq4Path),
                Occurrences("Alpha", mq4Path, MqlLanguage.Mql4, isDefinition: false));
            GlobalSymbolIndex.Instance.AddFile(
                mq5Path, MqlLanguage.Mql5,
                Definition("Alpha", mq5Path),
                Occurrences("Alpha", mq5Path, MqlLanguage.Mql5, isDefinition: false));

            var mql5 = GlobalSymbolIndex.Instance.FindOccurrences("Alpha", MqlLanguage.Mql5);
            Assert.Single(mql5);
            Assert.Equal(mq5Path, mql5[0].FilePath);

            var both = GlobalSymbolIndex.Instance.FindOccurrences("Alpha");
            Assert.Equal(2, both.Count);
        }

        [Fact]
        public void ReAddFile_ReplacesStaleOccurrences()
        {
            // OCC-03: re-indexing a file drops occurrences from the old content.
            const string path = "/idx/c.mq5";
            GlobalSymbolIndex.Instance.AddFile(
                path, MqlLanguage.Mql5,
                new List<MqlSymbol>(),
                new List<SymbolOccurrence>
                {
                    new SymbolOccurrence { FilePath = path, Language = MqlLanguage.Mql5, Text = "StaleName", Line = 0, Column = 0, Length = 9 }
                });

            // Re-index with new content: only FreshName.
            GlobalSymbolIndex.Instance.AddFile(
                path, MqlLanguage.Mql5,
                new List<MqlSymbol>(),
                new List<SymbolOccurrence>
                {
                    new SymbolOccurrence { FilePath = path, Language = MqlLanguage.Mql5, Text = "FreshName", Line = 1, Column = 2, Length = 9 }
                });

            var stale = GlobalSymbolIndex.Instance.FindOccurrences("StaleName");
            Assert.DoesNotContain(stale, o => o.FilePath == path);

            var fresh = GlobalSymbolIndex.Instance.FindOccurrences("FreshName");
            Assert.Contains(fresh, o => o.FilePath == path);
        }

        [Fact]
        public void ReAddFile_ReplacesOnlyOwnFileOccurrences()
        {
            // REQ-SM-04: occurrence replacement is scoped to the re-indexed file.
            const string pathA = "/idx/d_a.mq5";
            const string pathB = "/idx/d_b.mq5";
            GlobalSymbolIndex.Instance.AddFile(
                pathA, MqlLanguage.Mql5, new List<MqlSymbol>(),
                Occurrences("Shared", pathA, MqlLanguage.Mql5, isDefinition: false));
            GlobalSymbolIndex.Instance.AddFile(
                pathB, MqlLanguage.Mql5, new List<MqlSymbol>(),
                Occurrences("Shared", pathB, MqlLanguage.Mql5, isDefinition: false));

            // Re-index file A only, with no occurrences of Shared.
            GlobalSymbolIndex.Instance.AddFile(pathA, MqlLanguage.Mql5, new List<MqlSymbol>(), new List<SymbolOccurrence>());

            var remaining = GlobalSymbolIndex.Instance.FindOccurrences("Shared");
            Assert.Single(remaining);
            Assert.Equal(pathB, remaining[0].FilePath);
        }

        [Fact]
        public void FindOccurrences_MarksDefinitionViaSelectionRangeOverlap()
        {
            // OCC-04/D2: occurrence whose (Line, Column) falls inside the
            // definition's SelectionRange is stored with IsDefinition=true.
            const string path = "/idx/e.mq5";
            var symbols = Definition("Marked", path);

            var occurrences = new List<SymbolOccurrence>
            {
                // (0,4) is inside the SelectionRange (0,4)-(0,9) -> definition.
                new SymbolOccurrence { FilePath = path, Language = MqlLanguage.Mql5, Text = "Marked", Line = 0, Column = 4, Length = 5, IsDefinition = false },
                // (3,10) is outside any definition -> reference.
                new SymbolOccurrence { FilePath = path, Language = MqlLanguage.Mql5, Text = "Marked", Line = 3, Column = 10, Length = 5, IsDefinition = false }
            };

            GlobalSymbolIndex.Instance.AddFile(path, MqlLanguage.Mql5, symbols, occurrences);

            var results = GlobalSymbolIndex.Instance.FindOccurrences("Marked");
            Assert.Equal(2, results.Count);
            Assert.Contains(results, o => o.Line == 0 && o.Column == 4 && o.IsDefinition);
            Assert.Contains(results, o => o.Line == 3 && o.Column == 10 && !o.IsDefinition);
        }

        [Fact]
        public void FindOccurrences_BuiltinNameReturnsCodeOccurrencesOnly()
        {
            // OCC-05: builtin name (Period) is not special-cased; it behaves
            // like any name-keyed lookup. Only code occurrences are stored by
            // construction, so the query returns code positions only.
            const string path = "/idx/f.mq4";
            GlobalSymbolIndex.Instance.AddFile(
                path, MqlLanguage.Mql4, new List<MqlSymbol>(),
                new List<SymbolOccurrence>
                {
                    new SymbolOccurrence { FilePath = path, Language = MqlLanguage.Mql4, Text = "Period", Line = 2, Column = 4, Length = 6, IsDefinition = false }
                });

            var results = GlobalSymbolIndex.Instance.FindOccurrences("Period");
            Assert.Single(results);
            Assert.Equal(2, results[0].Line);
        }

        [Fact]
        public void RemoveFile_RemovesOccurrencesSymmetrically()
        {
            const string path = "/idx/g.mq5";
            GlobalSymbolIndex.Instance.AddFile(
                path, MqlLanguage.Mql5, new List<MqlSymbol>(),
                Occurrences("Gone", path, MqlLanguage.Mql5, isDefinition: false));

            GlobalSymbolIndex.Instance.RemoveFile(path, MqlLanguage.Mql5);

            Assert.DoesNotContain(GlobalSymbolIndex.Instance.FindOccurrences("Gone"), o => o.FilePath == path);
        }
    }
}
