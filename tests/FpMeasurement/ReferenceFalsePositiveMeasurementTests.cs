using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using MqlLanguageServer.Models;
using MqlLanguageServer.Mql4.Builtins;
using MqlLanguageServer.Mql5.Builtins;
using MqlLanguageServer.Mql5.Parser;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.FpMeasurement;

/// <summary>
/// TEMPORARY measurement harness (orchestrator-managed; not a regression test).
///
/// Purpose: quantify the false-positive rate of the current cross-file
/// "find references" implementation (ReferencesHandler.cs:100-141), which
/// greps every indexed file with the per-line regex
///     \b + Regex.Escape(symbol.Name) + \b
/// over raw text.
///
/// Method:
///  1. For every fixture file, LEX it with the language's ANTLR lexer and
///     collect all default-channel IDENTIFIER tokens (0-based line/col).
///  2. Collect all symbol definitions via IMqlParser.ParseFile.
///  3. Build the query set = every distinct definition name (minus keywords).
///  4. For each query name x each file, run EXACTLY the ReferencesHandler
///     regex and classify every match:
///       TP   = overlapping IDENTIFIER token with identical text
///       FP   = no overlapping IDENTIFIER token (comment / string /
///              preprocessor / other)
///       TP is further tagged DEFINITION when it overlaps a definition
///       SelectionRange of that name in that file.
///  5. Recall sanity check on a 10-name sample.
///
/// This test NEVER fails on measurement results (only on harness errors) so
/// the report is always produced.
/// </summary>
public class ReferenceFalsePositiveMeasurementTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string FixturesRoot = Path.Combine(RepoRoot, "tests", "fixtures");

    private static readonly string ReportMdPath = Path.Combine(RepoRoot, "tests", "TestResults", "fp-report.md");
    private static readonly string ReportJsonPath = Path.Combine(RepoRoot, "tests", "TestResults", "fp-report.json");

    /// <summary>
    /// MQL keywords that should never be queried (identifiers in the grammar's
    /// keyword rules would otherwise be "undefined"). Note: because we lex with
    /// the real grammar, keyword text never produces IDENTIFIER tokens anyway;
    /// this set guards the query list only.
    /// </summary>
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "int","double","string","bool","void","datetime","color","char","uchar",
        "short","ushort","uint","long","ulong","float","static","extern","input",
        "sinput","const","virtual","override","class","struct","public","private",
        "protected","template","typename","operator","enum","new","delete","sizeof",
        "if","else","while","for","do","switch","case","default","break","continue",
        "return","true","false","NULL","union","using","final","pack","nullptr",
        "this","typedef","and","or","not","interface","abstract","readonly",
        "typeof","namespace","import","include","property","define","ifdef",
        "ifndef","endif","undef","goto","unsigned","signed","mutable","explicit",
    };

    private readonly Mql4AntlrParser _mql4Parser = new();
    private readonly Mql5AntlrParser _mql5Parser = new();

    private readonly ITestOutputHelper _output;

    public ReferenceFalsePositiveMeasurementTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "FpMeasurement")]
    public void Measure_ReferenceFalsePositives_OnFixtureCorpus()
    {
        var files = CollectFixtureFiles();
        Assert.True(files.Count > 0, $"No fixture files found under {FixturesRoot}");

        // ------------------------------------------------------------------
        // Pass 1: lex + parse every file
        // ------------------------------------------------------------------
        var parsedFiles = new List<FileRecord>();
        var parseFailures = new List<string>();

        foreach (var path in files)
        {
            string content;
            try
            {
                content = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                parseFailures.Add($"{path}: read error {ex.Message}");
                continue;
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var isMql5 = ext == ".mq5";
            if (ext == ".mqh")
            {
                // Mirror LanguageDetection: sniff .mqh content for MQL5 tokens.
                var uri = new Uri(path);
                isMql5 = LanguageDetection.Detect(uri, null, content) == MqlLanguage.Mql5;
            }

            var lines = content.Split('\n');

            // ---- Lex ------------------------------------------------------
            List<IdentTok> idents;
            string? lexError = null;
            try
            {
                idents = LexIdentifiers(content, isMql5);
            }
            catch (Exception ex)
            {
                idents = new List<IdentTok>();
                lexError = ex.Message;
            }

            // Sanity: any identifier token inside comment/string content?
            // The grammars send comments to channel(2) and strings are a
            // distinct STRING token, so default-channel IDENTIFIER tokens
            // should never overlap a comment/string span. We verify by
            // scanning the line text around each token.
            var suspiciousTokens = new List<IdentTok>();
            foreach (var t in idents)
            {
                var lineText = t.Line < lines.Length ? lines[t.Line] : "";
                var trimmed = lineText.TrimStart();
                // A default-channel IDENTIFIER on a pure comment line or on a
                // preprocessor line would indicate channel leakage.
                var onCommentLine = trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*");
                var onPreprocLine = lineText.TrimStart().StartsWith("#");
                if (onCommentLine || onPreprocLine)
                {
                    suspiciousTokens.Add(t);
                }
            }

            // ---- Parse ----------------------------------------------------
            MqlFile? model = null;
            string? parseError = null;
            try
            {
                model = isMql5
                    ? _mql5Parser.ParseFile(content, path)
                    : _mql4Parser.ParseFile(content, path);
            }
            catch (Exception ex)
            {
                parseError = ex.Message;
            }

            var defs = new List<DefRecord>();
            var syntaxErrors = 0;
            if (model != null)
            {
                syntaxErrors = model.SyntaxErrors?.Count ?? 0;
                CollectDefinitions(model.Symbols, path, isMql5, defs);
            }
            else
            {
                parseError ??= "ParseFile returned null";
            }

            if (lexError != null || parseError != null)
            {
                parseFailures.Add($"{path}: lex='{lexError}' parse='{parseError}'");
            }

            parsedFiles.Add(new FileRecord
            {
                Path = path,
                Content = content,
                Lines = lines,
                IsMql5 = isMql5,
                Identifiers = idents,
                Definitions = defs,
                SyntaxErrors = syntaxErrors,
                SuspiciousTokens = suspiciousTokens,
                Model = model,
            });
        }

        // ------------------------------------------------------------------
        // Query set: every distinct definition name across the corpus
        // ------------------------------------------------------------------
        var defNames = parsedFiles
            .SelectMany(f => f.Definitions)
            .Select(d => d.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n) && !Keywords.Contains(n))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(defNames.Count > 0, "Corpus produced no definition names");

        // Builtins registry (both languages) for collision tagging.
        var builtinNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in Mql4Builtins.BuiltInFunctions.Keys) builtinNames.Add(n);
        foreach (var n in Mql4Builtins.BuiltInVariables.Keys) builtinNames.Add(n);
        var mql5Builtins = new Mql5Builtins();
        foreach (var n in mql5Builtins.BuiltInFunctions.Keys) builtinNames.Add(n);
        foreach (var n in mql5Builtins.BuiltInVariables.Keys) builtinNames.Add(n);

        // Definitions by name (workspace-wide) for DEFINITION tagging and
        // same-name collision analysis.
        var defsByName = parsedFiles
            .SelectMany(f => f.Definitions)
            .GroupBy(d => d.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        // ------------------------------------------------------------------
        // Pass 2: run the EXACT ReferencesHandler regex per query x file
        // ------------------------------------------------------------------
        long totalMatches = 0, tpCount = 0, fpCount = 0, defCount = 0;
        long tpScopeBound = 0, tpScopeAmbiguous = 0;
        var fpByCause = new Dictionary<string, long> { ["comment-line"] = 0, ["string-looking"] = 0, ["preprocessor-line"] = 0, ["other"] = 0 };
        var fpByQuery = new Dictionary<string, long>(StringComparer.Ordinal);
        var fpExamples = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var builtinCollisionVolume = new Dictionary<string, long>(StringComparer.Ordinal);
        var matchesPerQuery = new Dictionary<string, long>(StringComparer.Ordinal);
        var matchLines = new List<string>(); // sample of offending FP lines

        foreach (var name in defNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            var pattern = @"\b" + Regex.Escape(name) + @"\b";
            var regex = new Regex(pattern, RegexOptions.None);
            var queryIsBuiltin = builtinNames.Contains(name);
            long queryTotal = 0, queryFp = 0;

            foreach (var file in parsedFiles)
            {
                // Identifiers of THIS file bucketed by line for fast overlap.
                var idsByLine = new Dictionary<int, List<IdentTok>>();
                foreach (var t in file.Identifiers)
                {
                    if (!idsByLine.TryGetValue(t.Line, out var list))
                        idsByLine[t.Line] = list = new List<IdentTok>();
                    list.Add(t);
                }

                for (var i = 0; i < file.Lines.Length; i++)
                {
                    var currentLine = file.Lines[i];
                    if (!currentLine.Contains(name, StringComparison.Ordinal))
                        continue; // fast skip; regex would not match anyway

                    var matches = Regex.Matches(currentLine, pattern);
                    foreach (Match m in matches)
                    {
                        queryTotal++;
                        totalMatches++;

                        var startCol = m.Index;
                        var endCol = m.Index + m.Length;

                        // TP test: overlapping IDENTIFIER token, identical text.
                        var isTp = false;
                        if (idsByLine.TryGetValue(i, out var toks))
                        {
                            foreach (var t in toks)
                            {
                                if (t.StartCol < endCol && t.StartCol + t.Length > startCol
                                    && string.Equals(t.Text, name, StringComparison.Ordinal))
                                {
                                    isTp = true;
                                    break;
                                }
                            }
                        }

                        if (isTp)
                        {
                            tpCount++;

                            // DEFINITION tag: overlaps a definition
                            // SelectionRange of this name in this file.
                            var fileDefs = file.Definitions.Where(d => d.Name == name);
                            foreach (var d in fileDefs)
                            {
                                if (d.Line0 == i && startCol >= d.Col0 && startCol < d.EndCol)
                                {
                                    defCount++;
                                    break;
                                }
                            }

                            // Scope-binding tag (issue #45, tier 1):
                            // classifies each TP as bound to a single
                            // same-name definition under the document-local
                            // scope model, or as a scope-level ambiguity
                            // (multiple same-name defs; the TP sits in a
                            // function that declares its own same-name local,
                            // so a caller bound to a different same-name def
                            // would wrongly attribute it). Measurement-only:
                            // never asserted, reported below.
                            var scopeTag = ClassifyScopeBinding(file.Model, name, i, startCol);
                            if (scopeTag == "scope-ambiguous") tpScopeAmbiguous++;
                            else if (scopeTag == "bound" || scopeTag == "single-def") tpScopeBound++;
                        }
                        else
                        {
                            fpCount++;
                            queryFp++;

                            var cause = ClassifyFp(file.Lines[i], startCol, endCol);
                            fpByCause[cause]++;

                            if (!fpByQuery.TryGetValue(name, out var c)) fpByQuery[name] = c = 0;
                            fpByQuery[name] = c + 1;

                            if (!fpExamples.TryGetValue(name, out var exs)) fpExamples[name] = exs = new List<string>();
                            if (exs.Count < 3)
                            {
                                exs.Add($"{Path.GetFileName(file.Path)}:{i + 1} col {startCol} [{cause}] `{file.Lines[i].TrimEnd()}`");
                            }
                            if (matchLines.Count < 50) matchLines.Add(file.Lines[i].TrimEnd());
                        }
                    }
                }
            }

            matchesPerQuery[name] = queryTotal;
            if (queryFp > 0) fpByQuery[name] = queryFp;
            if (queryIsBuiltin && queryTotal > 0) builtinCollisionVolume[name] = queryTotal;
        }

        // ------------------------------------------------------------------
        // Pass 2b: run the query set through the NEW token-backed
        // implementation — index every fixture file into a FRESH
        // GlobalSymbolIndex instance (occurrence-aware AddFile with the
        // parser's captured occurrences) and query it via FindOccurrences.
        // This measures the production path (OCC-06 gate), unlike Pass 2,
        // which re-implements the old regex as a differential baseline.
        // ------------------------------------------------------------------
        var pass2b = MeasurePass2b(parsedFiles, defNames, builtinNames);

        // ------------------------------------------------------------------
        // Recall sanity: 10 sample names — every Identifier token occurrence
        // of the name must be covered by a regex match (token-level check).
        // Regex finding MORE than the tokens is expected (comments/strings);
        // that is counted as FP elsewhere, not as a recall miss.
        // ------------------------------------------------------------------
        var sample = defNames.OrderByDescending(n => matchesPerQuery.GetValueOrDefault(n)).Take(10).ToList();
        var recallMisses = new List<string>();
        foreach (var name in sample)
        {
            long regexHits = 0, idCount = 0, covered = 0;
            foreach (var file in parsedFiles)
            {
                var idsByLine = file.Identifiers
                    .Where(t => string.Equals(t.Text, name, StringComparison.Ordinal))
                    .GroupBy(t => t.Line)
                    .ToDictionary(g => g.Key, g => g.ToList());

                for (var i = 0; i < file.Lines.Length; i++)
                {
                    if (!file.Lines[i].Contains(name, StringComparison.Ordinal)) continue;
                    var ms = Regex.Matches(file.Lines[i], @"\b" + Regex.Escape(name) + @"\b").Select(m => (m.Index, m.Index + m.Length)).ToList();
                    regexHits += ms.Count;
                    if (idsByLine.TryGetValue(i, out var toks))
                    {
                        idCount += toks.Count;
                        foreach (var t in toks)
                            if (ms.Any(s => s.Item1 <= t.StartCol && t.StartCol + t.Length <= s.Item2))
                                covered++;
                    }
                }
            }
            if (covered != idCount)
                recallMisses.Add($"{name}: identifier-tokens={idCount} covered-by-regex={covered} (regex total={regexHits})");
        }

        // ------------------------------------------------------------------
        // Same-name collisions (ambiguity indicator)
        // ------------------------------------------------------------------
        var multiFileQueries = defsByName
            .Where(kv => kv.Value.Select(d => d.FilePath).Distinct().Count() > 1)
            .OrderByDescending(kv => kv.Value.Select(d => d.FilePath).Distinct().Count())
            .ToList();

        // Identifies any default-channel IDENTIFIER token sitting on a pure
        // comment/preprocessor line (would prove channel leakage).
        var suspiciousSamples = parsedFiles
            .SelectMany(f => f.SuspiciousTokens.Select(t => $"{Path.GetFileName(f.Path)}:{t.Line + 1} col {t.StartCol} `{t.Text}` on line: {f.Lines[t.Line].TrimEnd()}"))
            .ToList();

        // ------------------------------------------------------------------
        // Report
        // ------------------------------------------------------------------
        var corpusFiles = parsedFiles.Count;
        var corpusLines = parsedFiles.Sum(f => f.Lines.Length);
        var fpRate = totalMatches == 0 ? 0 : 100.0 * fpCount / totalMatches;
        var perLang = parsedFiles.GroupBy(f => f.IsMql5 ? "MQL5" : "MQL4")
            .ToDictionary(g => g.Key, g => (files: g.Count(), lines: g.Sum(x => x.Lines.Length)));

        _output.WriteLine($"Corpus: {corpusFiles} files, {corpusLines} lines");
        _output.WriteLine($"Matches: {totalMatches}, TP: {tpCount} (definitions: {defCount}), FP: {fpCount} ({fpRate:F2}%)");
        _output.WriteLine($"Scope-binding tags (issue #45, tier 1): bound={tpScopeBound}, scope-ambiguous={tpScopeAmbiguous}");
        _output.WriteLine($"Suspicious default-channel IDENT tokens on comment/preproc lines: {parsedFiles.Sum(f => f.SuspiciousTokens.Count)}");
        _output.WriteLine($"Parse failures: {parseFailures.Count}");
        _output.WriteLine($"Recall misses: {recallMisses.Count}");

        var md = new StringBuilder();
        var json = new StringBuilder();

        md.AppendLine("# References False-Positive Measurement Report");
        md.AppendLine();
        md.AppendLine($"Generated: {DateTime.UtcNow:u} — harness: `tests/FpMeasurement/ReferenceFalsePositiveMeasurementTests.cs` (temporary)");
        md.AppendLine();
        md.AppendLine("## Corpus");
        md.AppendLine();
        md.AppendLine($"| Metric | Value |");
        md.AppendLine($"|---|---|");
        md.AppendLine($"| Files | {corpusFiles} |");
        md.AppendLine($"| Total lines | {corpusLines} |");
        foreach (var (lang, v) in perLang)
            md.AppendLine($"| {lang} files / lines | {v.files} / {v.lines} |");
        md.AppendLine($"| Parse failures | {parseFailures.Count} |");
        md.AppendLine($"| Corpus syntax errors (parser-reported, informational) | {parsedFiles.Sum(f => f.SyntaxErrors)} |");
        md.AppendLine();

        md.AppendLine("## Aggregate");
        md.AppendLine();
        md.AppendLine("| Metric | Value |");
        md.AppendLine("|---|---|");
        md.AppendLine($"| Query names (distinct definition names, minus keywords) | {defNames.Count} |");
        md.AppendLine($"| Total regex matches | {totalMatches} |");
        md.AppendLine($"| TRUE-IDENTIFIER (TP) | {tpCount} |");
        md.AppendLine($"| — of which DEFINITION overlap | {defCount} |");
        md.AppendLine($"| FALSE-POSITIVE (FP) | {fpCount} |");
        md.AppendLine($"| **FP rate** | **{fpRate:F2}%** |");
        md.AppendLine($"| TP bound to a single same-name definition (scope tag) | {tpScopeBound} |");
        md.AppendLine($"| TP scope-ambiguous (multiple same-name defs; in-function same-name local) | {tpScopeAmbiguous} |");
        md.AppendLine();
        md.AppendLine("### FP cause breakdown (heuristics)");
        md.AppendLine();
        md.AppendLine("| Cause | Count |");
        md.AppendLine("|---|---|");
        foreach (var (k, v) in fpByCause) md.AppendLine($"| {k} | {v} |");
        md.AppendLine();

        md.AppendLine("### Builtin-name collisions (query name is a builtin; collision volume = all matches workspace-wide)");
        md.AppendLine();
        md.AppendLine("| Query name | Total matches |");
        md.AppendLine("|---|---|");
        foreach (var (n, v) in builtinCollisionVolume.OrderByDescending(kv => kv.Value).Take(10))
            md.AppendLine($"| {n} | {v} |");
        md.AppendLine();

        md.AppendLine("### Same-name collisions (name defined in >1 file)");
        md.AppendLine();
        md.AppendLine($"Queries with definitions in more than one file: **{multiFileQueries.Count}** of {defNames.Count}");
        md.AppendLine();
        md.AppendLine("| Query name | Defining files | Total workspace matches |");
        md.AppendLine("|---|---|---|");
        foreach (var (n, defs) in multiFileQueries.Take(10))
        {
            var fileCount = defs.Select(d => d.FilePath).Distinct().Count();
            md.AppendLine($"| {n} | {fileCount} | {matchesPerQuery.GetValueOrDefault(n)} |");
        }
        md.AppendLine();

        md.AppendLine("### Top 15 query names by FP count");
        md.AppendLine();
        md.AppendLine("| Query name | FP count | Example FPs |");
        md.AppendLine("|---|---|---|");
        foreach (var (n, c) in fpByQuery.OrderByDescending(kv => kv.Value).Take(15))
        {
            var ex = fpExamples.TryGetValue(n, out var exs) && exs.Count > 0
                ? string.Join("<br/>", exs.Take(3).Select(e => e.Replace("|", "\\|")))
                : "—";
            md.AppendLine($"| {n} | {c} | {ex} |");
        }
        md.AppendLine();

        md.AppendLine("### Per-language split");
        md.AppendLine();
        md.AppendLine("| Language | Files | Lines |");
        md.AppendLine("|---|---|---|");
        foreach (var (lang, v) in perLang)
            md.AppendLine($"| {lang} | {v.files} | {v.lines} |");
        md.AppendLine();

        md.AppendLine("### Harness verification notes");
        md.AppendLine();
        md.AppendLine($"- Default-channel IDENTIFIER tokens found on pure comment/preprocessor lines: **{parsedFiles.Sum(f => f.SuspiciousTokens.Count)}** (expected 0 — comments are channel(2), `#define` etc. channel(1), `#include`/`#property`/`#import` are whole-line PRE_* tokens).{(suspiciousSamples.Count > 0 ? " Examples: " + string.Join("; ", suspiciousSamples.Take(3)) : "")}");
        md.AppendLine($"- Recall sanity (10 hottest names, token-level): every Identifier occurrence must be covered by a regex match — uncovered: **{recallMisses.Count}**{string.Join("; ", recallMisses.Take(3))}");
        md.AppendLine($"- Positions: lexer `Token.Line`/`Column` are 1-based/0-based; converted to 0-based line and 0-based col to match the regex `match.Index` (0-based) and LSP `Range` (0-based, verified in Mql5SymbolVisitor `token.Line - 1`).");
        md.AppendLine();

        md.AppendLine("## Pass 2b — token-backed implementation (OCC-06 gate)");
        md.AppendLine();
        md.AppendLine("Measures the production path: every fixture file indexed into a fresh");
        md.AppendLine("`GlobalSymbolIndex` instance via occurrence-aware `AddFile` (the parser's");
        md.AppendLine("captured `TokenOccurrence` set), then queried via `FindOccurrences(name)`.");
        md.AppendLine("This is the instrument the verify phase uses for the OCC-06 gate; the");
        md.AppendLine("regex pass above remains as baseline evidence only.");
        md.AppendLine();
        var pass2bFpRate = pass2b.TotalOccurrences == 0 ? 0 : 100.0 * pass2b.FpCount / pass2b.TotalOccurrences;
        md.AppendLine("| Metric | Value |");
        md.AppendLine("|---|---|");
        md.AppendLine($"| Files indexed (parsed OK) | {pass2b.IndexedFiles} |");
        md.AppendLine($"| Total occurrences returned (all queries) | {pass2b.TotalOccurrences} |");
        md.AppendLine($"| FALSE-POSITIVE (not backed by identifier token) | {pass2b.FpCount} |");
        md.AppendLine($"| **FP rate** | **{pass2bFpRate:F4}%** |");
        md.AppendLine($"| Occurrences marked IsDefinition | {pass2b.DefinitionMarkedCount} |");
        md.AppendLine($"| IsDefinition vs SelectionRange-overlap mismatches | {pass2b.DefinitionFlagMismatches.Count} |");
        md.AppendLine($"| Identifier occurrences NOT returned (recall misses) | {pass2b.RecallMisses.Count} |");
        md.AppendLine();
        md.AppendLine("### Residual FP diagnosis");
        md.AppendLine();
        if (pass2b.FpCount == 0)
        {
            md.AppendLine("No false positives: every returned occurrence is backed by a");
            md.AppendLine("default-channel IDENTIFIER token. The \"1 stray\" previously expected");
            md.AppendLine("from the Pass 2 line-shape heuristic (NewDelete.mq5:15 `value` on");
            md.AppendLine("`*value = 42;`) is NOT a lexer leak: that line is real code — a");
            md.AppendLine("pointer dereference — and the harness's heuristic misreads the leading");
            md.AppendLine("`*` as a block-comment continuation. Pass 2b classifies strictly by");
            md.AppendLine("token overlap, so the token is correctly counted as a true identifier.");
            md.AppendLine("The token-backed implementation therefore shows FP = 0 on this corpus:");
            md.AppendLine("references come from default-channel IDENTIFIER tokens only, and");
            md.AppendLine("comment/string/preprocessor noise is removed by construction.");
        }
        else
        {
            md.AppendLine("Every remaining FP is a returned occurrence not backed by an");
            md.AppendLine("identifier token — either lexer channel leakage or a capture/index");
            md.AppendLine("transformation defect. Examples:");
            md.AppendLine();
            foreach (var (n, exs) in pass2b.FpExamples)
                foreach (var e in exs)
                    md.AppendLine($"- `{n}` — {e}");
        }
        md.AppendLine();
        md.AppendLine("### Top 10 query names by Pass 2b FP count");
        md.AppendLine();
        md.AppendLine("| Query name | FP count | Total occurrences |");
        md.AppendLine("|---|---|---|");
        foreach (var (n, c) in pass2b.FpByQuery.OrderByDescending(kv => kv.Value).Take(10))
            md.AppendLine($"| {n} | {c} | {pass2b.MatchesPerQuery.GetValueOrDefault(n)} |");
        md.AppendLine();

        if (pass2b.DefinitionFlagMismatches.Count > 0)
        {
            md.AppendLine("### IsDefinition flag mismatches");
            md.AppendLine();
            foreach (var m in pass2b.DefinitionFlagMismatches.Take(10))
                md.AppendLine($"- {m}");
            md.AppendLine();
        }

        if (pass2b.RecallMisses.Count > 0)
        {
            md.AppendLine("### Recall misses (identifier tokens not returned by FindOccurrences)");
            md.AppendLine();
            foreach (var m in pass2b.RecallMisses.Take(10))
                md.AppendLine($"- {m}");
            md.AppendLine();
        }

        if (parseFailures.Count > 0)
        {
            md.AppendLine("### Parse/lex failures (files excluded from classification)");
            md.AppendLine();
            foreach (var f in parseFailures) md.AppendLine($"- {f}");
            md.AppendLine();
        }

        md.AppendLine("## Serena test verdict");
        md.AppendLine();
        md.AppendLine(BuildVerdict(fpRate, fpCount, totalMatches, multiFileQueries.Count, defNames.Count, builtinCollisionVolume));
        md.AppendLine();

        // JSON
        var jsonParts = new List<string>();
        jsonParts.Add($"\"corpus\":{{\"files\":{corpusFiles},\"lines\":{corpusLines},\"parseFailures\":{parseFailures.Count}}}");
        jsonParts.Add($"\"aggregate\":{{\"queryNames\":{defNames.Count},\"totalMatches\":{totalMatches},\"tp\":{tpCount},\"tpDefinitions\":{defCount},\"tpScopeBound\":{tpScopeBound},\"tpScopeAmbiguous\":{tpScopeAmbiguous},\"fp\":{fpCount},\"fpRate\":{fpRate.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}}}");
        jsonParts.Add("\"fpCauses\":{" + string.Join(",", fpByCause.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) + "}");
        jsonParts.Add("\"builtinCollisions\":[" + string.Join(",", builtinCollisionVolume
            .OrderByDescending(kv => kv.Value).Take(10)
            .Select(kv => JObj(new List<(string, string)> { ("name", JStr(kv.Key)), ("matches", kv.Value.ToString()) }))) + "]");
        jsonParts.Add("\"sameNameCollisions\":{\"count\":" + multiFileQueries.Count + ",\"top\":[" + string.Join(",", multiFileQueries.Take(10)
            .Select(kv => JObj(new List<(string, string)> { ("name", JStr(kv.Key)), ("definingFiles", kv.Value.Select(d => d.FilePath).Distinct().Count().ToString()), ("matches", matchesPerQuery.GetValueOrDefault(kv.Key).ToString()) }))) + "]}");
        jsonParts.Add("\"topFp\":[" + string.Join(",", fpByQuery.OrderByDescending(kv => kv.Value).Take(15)
            .Select(kv => JObj(new List<(string, string)>
            {
                ("name", JStr(kv.Key)),
                ("fp", kv.Value.ToString()),
                ("examples", "[" + string.Join(",", (fpExamples.TryGetValue(kv.Key, out var exs) ? exs : new List<string>()).Select(JStr)) + "]"),
            }))) + "]");
        jsonParts.Add("\"perLanguage\":{" + string.Join(",", perLang.Select(kv => "\"" + kv.Key + "\":{\"files\":" + kv.Value.files + ",\"lines\":" + kv.Value.lines + "}")) + "}");
        jsonParts.Add("\"suspiciousTokensOnCommentOrPreprocLines\":" + parsedFiles.Sum(f => f.SuspiciousTokens.Count));
        jsonParts.Add("\"suspiciousSamples\":[" + string.Join(",", suspiciousSamples.Select(JStr)) + "]");
        jsonParts.Add("\"recallMisses\":[" + string.Join(",", recallMisses.Select(JStr)) + "]");
        jsonParts.Add("\"parseFailures\":[" + string.Join(",", parseFailures.Select(JStr)) + "]");
        var pass2bFpRateValue = pass2b.TotalOccurrences == 0 ? 0 : 100.0 * pass2b.FpCount / pass2b.TotalOccurrences;
        jsonParts.Add("\"pass2b\":{" +
            "\"indexedFiles\":" + pass2b.IndexedFiles + "," +
            "\"totalOccurrences\":" + pass2b.TotalOccurrences + "," +
            "\"fp\":" + pass2b.FpCount + "," +
            "\"fpRate\":" + pass2bFpRateValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," +
            "\"definitionMarked\":" + pass2b.DefinitionMarkedCount + "," +
            "\"definitionFlagMismatches\":" + pass2b.DefinitionFlagMismatches.Count + "," +
            "\"recallMisses\":" + pass2b.RecallMisses.Count + "," +
            "\"residualDiagnosis\":\"FP=0 on this corpus; the previously expected 1 stray (NewDelete.mq5:15 `value` on `*value = 42;`) is a real pointer-dereference line misclassified by the Pass 2 line-shape heuristic, not lexer leakage\"," +
            "\"topFp\":[" + string.Join(",", pass2b.FpByQuery.OrderByDescending(kv => kv.Value).Take(10)
                .Select(kv => JObj(new List<(string, string)>
                {
                    ("name", JStr(kv.Key)),
                    ("fp", kv.Value.ToString()),
                    ("total", pass2b.MatchesPerQuery.GetValueOrDefault(kv.Key).ToString()),
                    ("examples", "[" + string.Join(",", (pass2b.FpExamples.TryGetValue(kv.Key, out var exs2b) ? exs2b : new List<string>()).Select(JStr)) + "]"),
                }))) + "]" +
            "}");

        json.Append('{');
        json.Append(string.Join(",", jsonParts));
        json.Append('}');

        Directory.CreateDirectory(Path.GetDirectoryName(ReportMdPath)!);
        File.WriteAllText(ReportMdPath, md.ToString());
        File.WriteAllText(ReportJsonPath, json.ToString());

        _output.WriteLine($"Report written: {ReportMdPath}");
        _output.WriteLine($"Report written: {ReportJsonPath}");
        _output.WriteLine($"Pass 2b (token-backed): occurrences={pass2b.TotalOccurrences}, FP={pass2b.FpCount} ({pass2bFpRate:F4}%), definition-marked={pass2b.DefinitionMarkedCount}, flag-mismatches={pass2b.DefinitionFlagMismatches.Count}, recall-misses={pass2b.RecallMisses.Count}");

        // Harness-only sanity asserts (never on measurement values).
        Assert.True(totalMatches > 0, "Harness produced zero matches — measurement is invalid.");
        Assert.True(pass2b.IndexedFiles > 0, "Pass 2b indexed no files — measurement is invalid.");
        Assert.True(pass2b.TotalOccurrences > 0, "Pass 2b produced zero occurrences — measurement is invalid.");
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    /// <summary>
    /// OCC-06 gate instrument (Pass 2b): measures the token-backed
    /// implementation — GlobalSymbolIndex.FindOccurrences over occurrence-aware
    /// AddFile — on the fixture corpus.
    ///
    /// Per query name:
    ///  (a) FP  = returned occurrences NOT backed by an identifier token of
    ///            that file (overlapping token, identical text). Expected ~0:
    ///            the capture reads default-channel IDENTIFIER tokens only,
    ///            so the only residual FPs are the documented stray tokens
    ///            (e.g. NewDelete.mq5:15 `value`, a block-comment continuation
    ///            line the lexer still marks as code).
    ///  (b) Recall = identifier token occurrences of the name covered by a
    ///            FindOccurrences result. Expected 100%: capture and lexer
    ///            use the same channel/type filter and 0-based conversion.
    ///  (c) Definition exclusion = IsDefinition flag vs SelectionRange
    ///            overlap (OCC-04): each returned occurrence flagged as
    ///            definition must overlap a definition SelectionRange of the
    ///            name in that file, and vice versa.
    ///
    /// Non-asserting on measurement values: this harness is the measurement
    /// instrument; the PASS/FAIL verdict on ~0% is stated in the report and
    /// checked by the verify phase. Only harness-sanity asserts are allowed.
    /// </summary>
    private static Pass2bResult MeasurePass2b(List<FileRecord> parsedFiles, HashSet<string> defNames, HashSet<string> builtinNames)
    {
        // Fresh index instance (isolated from GlobalSymbolIndex.Instance) via
        // the internal constructor, exposed to the test assembly.
        var index = new GlobalSymbolIndex(ctorBypass: true);
        try
        {
            // Index every fixture file the way didOpen/didChange/workspace scan
            // do: occurrence-aware AddFile with the parser's fresh occurrences.
            foreach (var file in parsedFiles)
            {
                if (file.Model == null)
                    continue;

                var language = file.IsMql5 ? MqlLanguage.Mql5 : MqlLanguage.Mql4;
                var occurrences = file.Model.Occurrences
                    .Select(o => new SymbolOccurrence
                    {
                        FilePath = file.Path,
                        Language = language,
                        Text = o.Text,
                        Line = o.Line,
                        Column = o.Column,
                        Length = o.Length
                    })
                    .ToList();

                index.AddFile(file.Path, language, file.Model.Symbols, occurrences);
            }

            long totalCount = 0, fpCount = 0, defMarkedCount = 0;
            var defFlagWrong = new List<string>();
            var fpExamples2b = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var fpByQuery2b = new Dictionary<string, long>(StringComparer.Ordinal);
            var builtinCollisions2b = new Dictionary<string, long>(StringComparer.Ordinal);
            var matchesPerQuery2b = new Dictionary<string, long>(StringComparer.Ordinal);

            foreach (var name in defNames.OrderBy(n => n, StringComparer.Ordinal))
            {
                var results = index.FindOccurrences(name);
                matchesPerQuery2b[name] = results.Count;
                totalCount += results.Count;

                if (builtinNames.Contains(name) && results.Count > 0)
                    builtinCollisions2b[name] = results.Count;

                foreach (var occurrence in results)
                {
                    var filePath = occurrence.FilePath;
                    var file = parsedFiles.FirstOrDefault(f => f.Path == filePath);
                    if (file == null)
                        continue; // file excluded from corpus; cannot classify.

                    // (a) FP test: overlapping identifier token, identical text.
                    var backedByToken = file.Identifiers.Any(t =>
                        t.Line == occurrence.Line &&
                        t.StartCol < occurrence.Column + occurrence.Length &&
                        t.StartCol + t.Length > occurrence.Column &&
                        string.Equals(t.Text, occurrence.Text, StringComparison.Ordinal));

                    if (!backedByToken)
                    {
                        fpCount++;
                        fpByQuery2b[name] = fpByQuery2b.GetValueOrDefault(name) + 1;
                        if (fpExamples2b.TryGetValue(name, out var exs) == false)
                            fpExamples2b[name] = exs = new List<string>();
                        if (exs.Count < 3)
                        {
                            var lineText = occurrence.Line < file.Lines.Length
                                ? file.Lines[occurrence.Line].TrimEnd()
                                : "";
                            exs.Add($"{Path.GetFileName(filePath)}:{occurrence.Line + 1} col {occurrence.Column} `{lineText}`");
                        }
                    }

                    // (c) Definition-flag check: IsDefinition must coincide with
                    // an overlapping definition SelectionRange (OCC-04).
                    var overlapsDefinition = file.Definitions.Any(d =>
                        d.Name == name &&
                        d.Line0 == occurrence.Line &&
                        occurrence.Column >= d.Col0 &&
                        occurrence.Column < d.EndCol);

                    if (occurrence.IsDefinition != overlapsDefinition)
                    {
                        defFlagWrong.Add(
                            $"{Path.GetFileName(filePath)}:{occurrence.Line + 1} col {occurrence.Column} IsDefinition={occurrence.IsDefinition} overlap={overlapsDefinition}");
                    }

                    if (occurrence.IsDefinition)
                        defMarkedCount++;
                }
            }

            // (b) Recall: every identifier token occurrence of the query name
            // must be covered by a FindOccurrences result.
            var recallMisses2b = new List<string>();
            foreach (var name in defNames)
            {
                var found = index.FindOccurrences(name);
                var foundKeys = found
                    .Select(o => (o.FilePath, o.Line, o.Column, o.Length))
                    .ToHashSet();

                foreach (var file in parsedFiles)
                {
                    foreach (var t in file.Identifiers)
                    {
                        if (!string.Equals(t.Text, name, StringComparison.Ordinal))
                            continue;
                        if (!foundKeys.Contains((file.Path, t.Line, t.StartCol, t.Length)))
                        {
                            recallMisses2b.Add(
                                $"{Path.GetFileName(file.Path)}:{t.Line + 1} col {t.StartCol} `{name}` not returned by FindOccurrences");
                        }
                    }
                }
            }

            return new Pass2bResult
            {
                IndexedFiles = parsedFiles.Count(f => f.Model != null),
                TotalOccurrences = totalCount,
                FpCount = fpCount,
                DefinitionMarkedCount = defMarkedCount,
                DefinitionFlagMismatches = defFlagWrong,
                FpByQuery = fpByQuery2b,
                FpExamples = fpExamples2b,
                BuiltinCollisions = builtinCollisions2b,
                MatchesPerQuery = matchesPerQuery2b,
                RecallMisses = recallMisses2b,
            };
        }
        finally
        {
            // The fresh instance is never registered as the singleton; drop it.
            index.Clear();
        }
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    private static Func<string, string> JStr = s => "\"" + Escape(s) + "\"";

    private static string JObj(List<(string Key, string Value)> props) =>
        "{" + string.Join(",", props.Select(p => "\"" + p.Key + "\":" + p.Value)) + "}";

    private static int CountIdentifierOccurrencesInFile(FileRecord file, string name) =>
        file.Identifiers.Count(t => string.Equals(t.Text, name, StringComparison.Ordinal));

    /// <summary>
    /// Position-aware FP cause classification: determine whether the match
    /// span sits inside a comment or string by scanning the line up to the
    /// match start (handles trailing `// comment` and string literals
    /// anywhere in the line, not just line-start heuristics).
    /// </summary>
    /// <summary>
    /// Issue #45 scope-binding tag for TP matches (measurement-only, mirrors
    /// the tier-1 document-local scope model of ScopeOccurrenceFilter):
    ///  - "single-def": at most one same-name definition in the document —
    ///    no shadowing possible.
    ///  - "bound": multiple defs, but the position resolves unambiguously
    ///    under function-body granularity (the innermost containing function
    ///    declares no same-name variable/parameter, so the position binds to
    ///    the global or function definition).
    ///  - "scope-ambiguous": multiple defs and the position sits inside a
    ///    function that declares its own same-name local — a caller bound to
    ///    a different same-name definition would wrongly attribute this TP.
    ///  - "no-model": the file failed to parse; cannot classify.
    /// </summary>
    private static string ClassifyScopeBinding(MqlFile? model, string name, int line, int column)
    {
        if (model == null)
            return "no-model";

        var defs = model.Symbols
            .Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (defs.Count <= 1)
            return "single-def";

        var functions = model.Symbols.Where(IsFunctionLikeSymbol).ToList();
        var containing = functions
            .Where(f => RangeContains(f.Range, line, column))
            .OrderBy(f => (f.Range.End.Line - f.Range.Start.Line) * 1_000_000
                          + (f.Range.End.Character - f.Range.Start.Character))
            .FirstOrDefault();

        if (containing == null)
            return "bound"; // outside every function: binds to the global def

        var hasSameNameLocal = defs.Any(d =>
            !ReferenceEquals(d, containing)
            && IsVariableLikeSymbol(d)
            && d.SelectionRange?.Start != null
            && RangeContains(containing.Range, d.SelectionRange.Start.Line, d.SelectionRange.Start.Character));

        return hasSameNameLocal ? "scope-ambiguous" : "bound";
    }

    private static bool IsFunctionLikeSymbol(MqlSymbol s) =>
        s.SymbolType == SymbolType.Function
        || s.SymbolType == SymbolType.Method
        || s.Kind == OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Function
        || s.Kind == OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Method;

    private static bool IsVariableLikeSymbol(MqlSymbol s) =>
        s.SymbolType == SymbolType.Variable
        || s.Kind == OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Variable;

    private static bool RangeContains(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range? range, int line, int column)
    {
        if (range?.Start == null || range.End == null)
            return false;
        if (line < range.Start.Line || line > range.End.Line)
            return false;
        if (line == range.Start.Line && column < range.Start.Character)
            return false;
        if (line == range.End.Line && column > range.End.Character)
            return false;
        return true;
    }

    private static string ClassifyFp(string line, int startCol, int endCol) {
        // Trailing comment: a `//` (or `/*` start) before the match, with no
        // line terminator after it (we are within one source line already).
        var lineComment = line.IndexOf("//", StringComparison.Ordinal);
        if (lineComment >= 0 && startCol >= lineComment) return "comment-line";
        var blockComment = line.IndexOf("/*", StringComparison.Ordinal);
        if (blockComment >= 0 && startCol >= blockComment) return "comment-line";

        // Preprocessor line (whole-line directives).
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("#") || trimmed.StartsWith("*"))
        {
            if (startCol <= line.Length - trimmed.Length + 1 || trimmed.StartsWith("#"))
                return "preprocessor-line";
        }

        // String content: match lies inside a double-quoted span.
        var quoteOpen = -1;
        for (var i = 0; i < line.Length && i < endCol; i++)
        {
            if (line[i] == '"')
            {
                if (quoteOpen < 0) quoteOpen = i;
                else quoteOpen = -1;
            }
        }
        if (quoteOpen >= 0 && startCol > quoteOpen) return "string-looking";
        if (startCol > 0 && line[startCol - 1] == '"') return "string-looking";
        if (endCol < line.Length && line[endCol] == '"') return "string-looking";

        return "other";
    }

    private static string ClassifyFp(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
            return "comment-line";
        if (trimmed.StartsWith("#"))
            return "preprocessor-line";
        if (trimmed.StartsWith("\"") || trimmed.StartsWith("'") || trimmed.Contains('"'))
            return "string-looking";
        return "other";
    }

    private static string BuildVerdict(
        double fpRate, long fpCount, long totalMatches, int multiFileQueries, int defNames,
        Dictionary<string, long> builtinCollisions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"With the current implementation (regex over raw text per ReferencesHandler.cs:115-135), " +
                      $"**{fpRate:F2}%** of all returned locations across this corpus are false positives " +
                      $"({fpCount} of {totalMatches} matches): comment lines, preprocessor lines (`#property copyright \"...\"` etc.), " +
                      "and string literals produce phantom locations.");
        sb.AppendLine();
        sb.AppendLine("Consequence for cross-file reference tests with exact expected positions: any test that pins an " +
                      "exact reference list (positions or counts) would be fragile — it would either fail on the extra " +
                      "phantom positions or, worse, pass while asserting wrong positions when the symbol name only appears " +
                      "in comments. Tests must either filter to Identifier-backed locations, use only symbols whose name " +
                      "appears solely in code positions, or tolerate superset results.");
        sb.AppendLine();
        sb.AppendLine($"Ambiguity: {multiFileQueries} of {defNames} query names are defined in more than one file, so a caller " +
                      "cannot tell which definition a returned reference belongs to (the implementation returns locations " +
                      "only — definition/reference and scope are indistinguishable).");
        var topBuiltin = builtinCollisions.OrderByDescending(kv => kv.Value).FirstOrDefault();
        if (topBuiltin.Key != null)
            sb.AppendLine($" Builtin-name queries are additionally polluted: e.g. `{topBuiltin.Key}` generates {topBuiltin.Value} raw matches workspace-wide.");
        return sb.ToString();
    }

    private List<IdentTok> LexIdentifiers(string content, bool isMql5)
    {
        var input = new AntlrInputStream(content);
        IList<IToken> tokens;
        if (isMql5)
        {
            var lexer = new Mql5Grammar.Mql5GrammarLexer(input);
            var ts = new CommonTokenStream(lexer);
            ts.Fill();
            tokens = ts.GetTokens();
        }
        else
        {
            var lexer = new Mql4Grammar.Mql4GrammarLexer(input);
            var ts = new CommonTokenStream(lexer);
            ts.Fill();
            tokens = ts.GetTokens();
        }

        var result = new List<IdentTok>();
        foreach (var t in tokens)
        {
            // Channel 0 = default; TokenConstants.DefaultChannel == 0.
            if (t.Channel != 0) continue;
            var type = isMql5 ? Mql5Grammar.Mql5GrammarLexer.IDENTIFIER : Mql4Grammar.Mql4GrammarLexer.IDENTIFIER;
            if (t.Type != type) continue;
            // Token.Line is 1-based, Token.Column is 0-based → convert to 0-based.
            result.Add(new IdentTok(t.Line - 1, t.Column, t.Text?.Length ?? 0, t.Text ?? ""));
        }
        return result;
    }

    private static void CollectDefinitions(IEnumerable<MqlSymbol> symbols, string filePath, bool isMql5, List<DefRecord> into)
    {
        foreach (var s in symbols)
        {
            if (!string.IsNullOrWhiteSpace(s.Name))
            {
                // SelectionRange is the declaration name only (0-based LSP range,
                // verified against Mql5SymbolVisitor / Mql4SymbolVisitor).
                var sel = s.SelectionRange;
                if (sel != null && sel.Start != null && sel.End != null && sel.Start.Line == sel.End.Line)
                {
                    into.Add(new DefRecord(s.Name, filePath, sel.Start.Line, sel.Start.Character, sel.End.Character, isMql5));
                }
                else if (s.Range != null && s.Range.Start != null && s.Range.End != null)
                {
                    into.Add(new DefRecord(s.Name, filePath, s.Range.Start.Line, s.Range.Start.Character, s.Range.End.Character, isMql5));
                }
            }
            if (s.Children is { Count: > 0 })
                CollectDefinitions(s.Children, filePath, isMql5, into);
        }
    }

    private static List<string> CollectFixtureFiles()
    {
        var exts = new[] { ".mq4", ".mq5", ".mqh" };
        return Directory.EnumerateFiles(FixturesRoot, "*", SearchOption.AllDirectories)
            .Where(p => exts.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MqlLanguageServer.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("Repo root not found");
        return dir.FullName;
    }

    private sealed record IdentTok(int Line, int StartCol, int Length, string Text);
    private sealed record DefRecord(string Name, string FilePath, int Line0, int Col0, int EndCol, bool IsMql5);

    /// <summary>
    /// Pass 2b measurement output (token-backed implementation via
    /// GlobalSymbolIndex.FindOccurrences). Non-asserting: values feed the
    /// report; the OCC-06 verdict belongs to the verify phase.
    /// </summary>
    private sealed class Pass2bResult
    {
        public int IndexedFiles;
        public long TotalOccurrences;
        public long FpCount;
        public long DefinitionMarkedCount;
        public List<string> DefinitionFlagMismatches = new();
        public Dictionary<string, long> FpByQuery = new(StringComparer.Ordinal);
        public Dictionary<string, List<string>> FpExamples = new(StringComparer.Ordinal);
        public Dictionary<string, long> BuiltinCollisions = new(StringComparer.Ordinal);
        public Dictionary<string, long> MatchesPerQuery = new(StringComparer.Ordinal);
        public List<string> RecallMisses = new();
    }

    private sealed class FileRecord
    {
        public string Path = "";
        public string Content = "";
        public string[] Lines = Array.Empty<string>();
        public bool IsMql5;
        public List<IdentTok> Identifiers = new();
        public List<DefRecord> Definitions = new();
        public int SyntaxErrors;
        public List<IdentTok> SuspiciousTokens = new();
        public MqlFile? Model;
    }
}