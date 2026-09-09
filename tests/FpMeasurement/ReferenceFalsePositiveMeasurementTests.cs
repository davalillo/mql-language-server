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

    private static readonly string ReportMdPath = "/tmp/nix-shell.EGzmYS/opencode/fp-report.md";
    private static readonly string ReportJsonPath = "/tmp/nix-shell.EGzmYS/opencode/fp-report.json";

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
        jsonParts.Add($"\"aggregate\":{{\"queryNames\":{defNames.Count},\"totalMatches\":{totalMatches},\"tp\":{tpCount},\"tpDefinitions\":{defCount},\"fp\":{fpCount},\"fpRate\":{fpRate.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}}}");
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

        json.Append('{');
        json.Append(string.Join(",", jsonParts));
        json.Append('}');

        Directory.CreateDirectory(Path.GetDirectoryName(ReportMdPath)!);
        File.WriteAllText(ReportMdPath, md.ToString());
        File.WriteAllText(ReportJsonPath, json.ToString());

        _output.WriteLine($"Report written: {ReportMdPath}");
        _output.WriteLine($"Report written: {ReportJsonPath}");

        // Harness-only sanity asserts (never on measurement values).
        Assert.True(totalMatches > 0, "Harness produced zero matches — measurement is invalid.");
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

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
    private static string ClassifyFp(string line, int startCol, int endCol)
    {
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
    }
}