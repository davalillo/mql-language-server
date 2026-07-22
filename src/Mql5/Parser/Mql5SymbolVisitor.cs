using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Mql5Grammar;
using MqlLanguageServer.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspPosition = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace MqlLanguageServer.Mql5.Parser;

/// <summary>
/// ANTLR visitor that walks the Mql5Grammar parse tree and extracts MqlSymbol instances.
/// Mirrors Mql4SymbolVisitor structure and adds MQL5-specific hierarchy support.
/// </summary>
public class Mql5SymbolVisitor : Mql5GrammarBaseVisitor<MqlSymbol?>
{
    public List<MqlSymbol> Symbols { get; } = new List<MqlSymbol>();
    public List<string> Includes { get; } = new List<string>();

    private readonly string _filePath;
    private readonly Dictionary<string, MqlSymbol> _symbolsByName = new(StringComparer.OrdinalIgnoreCase);

    public Mql5SymbolVisitor(string filePath)
    {
        _filePath = filePath ?? string.Empty;
    }

    public override MqlSymbol? VisitClassDeclaration([NotNull] Mql5GrammarParser.ClassDeclarationContext context)
    {
        var nameToken = context.IDENTIFIER()?.Symbol;
        if (nameToken == null)
            return base.VisitClassDeclaration(context);

        var name = nameToken.Text;
        var symbol = CreateTypeSymbol(name, nameToken, SymbolType.Class);

        if (context.accessModifier() != null && context.qualifiedName() != null)
        {
            var parentName = context.qualifiedName().GetText();
            symbol.Detail = $"class {name} : {parentName}";
        }

        Symbols.Add(symbol);
        _symbolsByName[name] = symbol;

        var result = base.VisitClassDeclaration(context);
        ResolveHierarchy();
        return result;
    }

    public override MqlSymbol? VisitStructDeclaration([NotNull] Mql5GrammarParser.StructDeclarationContext context)
    {
        var nameToken = context.IDENTIFIER()?.Symbol;
        if (nameToken == null)
            return base.VisitStructDeclaration(context);

        var name = nameToken.Text;
        var symbol = CreateTypeSymbol(name, nameToken, SymbolType.Struct);

        if (context.accessModifier() != null && context.qualifiedName() != null)
        {
            var parentName = context.qualifiedName().GetText();
            symbol.Detail = $"struct {name} : {parentName}";
        }

        Symbols.Add(symbol);
        _symbolsByName[name] = symbol;

        var result = base.VisitStructDeclaration(context);
        ResolveHierarchy();
        return result;
    }

    public override MqlSymbol? VisitInterfaceDeclaration([NotNull] Mql5GrammarParser.InterfaceDeclarationContext context)
    {
        var nameToken = context.IDENTIFIER()?.Symbol;
        if (nameToken == null)
            return base.VisitInterfaceDeclaration(context);

        var name = nameToken.Text;
        var symbol = CreateTypeSymbol(name, nameToken, SymbolType.Interface);

        if (context.accessModifier() != null && context.qualifiedName() != null)
        {
            var parentName = context.qualifiedName().GetText();
            symbol.Detail = $"interface {name} : {parentName}";
        }

        Symbols.Add(symbol);
        _symbolsByName[name] = symbol;

        var result = base.VisitInterfaceDeclaration(context);
        ResolveHierarchy();
        return result;
    }

    public override MqlSymbol? VisitUnionDeclaration([NotNull] Mql5GrammarParser.UnionDeclarationContext context)
    {
        var nameToken = context.IDENTIFIER()?.Symbol;
        if (nameToken == null)
            return base.VisitUnionDeclaration(context);

        var name = nameToken.Text;
        var symbol = CreateTypeSymbol(name, nameToken, SymbolType.Struct);
        symbol.Detail = $"union {name}";

        Symbols.Add(symbol);
        _symbolsByName[name] = symbol;

        return base.VisitUnionDeclaration(context);
    }

    public override MqlSymbol? VisitEnumDeclaration([NotNull] Mql5GrammarParser.EnumDeclarationContext context)
    {
        var nameToken = context.IDENTIFIER()?.Symbol;
        if (nameToken == null)
            return base.VisitEnumDeclaration(context);

        var name = nameToken.Text;
        var range = CreateRangeFromToken(nameToken);
        var symbol = new MqlSymbol
        {
            Name = name,
            Kind = SymbolType.Enum.ToLspSymbolKind(),
            SymbolType = SymbolType.Enum,
            Range = range,
            SelectionRange = range,
            Detail = context.K_CLASS() != null ? $"enum class {name}" : $"enum {name}",
            FilePath = _filePath
        };

        Symbols.Add(symbol);
        _symbolsByName[name] = symbol;

        return base.VisitEnumDeclaration(context);
    }

    public override MqlSymbol? VisitFunctionDeclaration([NotNull] Mql5GrammarParser.FunctionDeclarationContext context)
    {
        var name = context.qualifiedName()?.GetText();
        if (!string.IsNullOrEmpty(name))
        {
            var lastToken = context.qualifiedName().IDENTIFIER(context.qualifiedName().IDENTIFIER().Length - 1);
            var selectionRange = CreateRangeFromToken(lastToken.Symbol);
            var fullRange = CreateFullFunctionRange(context, lastToken.Symbol);

            var symbol = new MqlSymbol
            {
                Name = name,
                Kind = SymbolType.Function.ToLspSymbolKind(),
                SymbolType = SymbolType.Function,
                Range = fullRange,
                Detail = $"function {context.type().GetText()} {name}",
                SelectionRange = selectionRange,
                FilePath = _filePath
            };

            Symbols.Add(symbol);
        }

        return base.VisitFunctionDeclaration(context);
    }

    public override MqlSymbol? VisitVariableDeclaration([NotNull] Mql5GrammarParser.VariableDeclarationContext context)
    {
        var declarators = context.variableDeclarator();
        if (declarators != null && declarators.Length > 0)
        {
            var firstDeclarator = declarators[0];
            var nameToken = firstDeclarator.IDENTIFIER();
            if (nameToken != null)
            {
                var name = nameToken.GetText();
                var range = CreateRangeFromToken(nameToken.Symbol);

                var modifierTokens = new List<string>();
                for (int i = 0; i < context.modifiers().Length; i++)
                {
                    var modifiersContext = context.modifiers(i);
                    if (modifiersContext.K_INPUT() != null) modifierTokens.Add("input");
                    if (modifiersContext.K_EXTERN() != null) modifierTokens.Add("extern");
                    if (modifiersContext.K_STATIC() != null) modifierTokens.Add("static");
                    if (modifiersContext.K_CONST() != null) modifierTokens.Add("const");
                    if (modifiersContext.K_SINPUT() != null) modifierTokens.Add("sinput");
                    if (modifiersContext.K_VIRTUAL() != null) modifierTokens.Add("virtual");
                    if (modifiersContext.K_FINAL() != null) modifierTokens.Add("final");
                }

                string modifier = string.Join(" ", modifierTokens);
                var typeText = context.type().GetText();
                string detail = string.IsNullOrEmpty(modifier)
                    ? $"{typeText} {name}"
                    : $"{modifier} {typeText} {name}";

                var symbol = new MqlSymbol
                {
                    Name = name,
                    Kind = SymbolType.Variable.ToLspSymbolKind(),
                    SymbolType = SymbolType.Variable,
                    Range = range,
                    SelectionRange = range,
                    Detail = detail,
                    FilePath = _filePath
                };

                Symbols.Add(symbol);
            }
        }

        return base.VisitVariableDeclaration(context);
    }

    public override MqlSymbol? VisitDirective([NotNull] Mql5GrammarParser.DirectiveContext context)
    {
        if (context.PRE_INCLUDE() != null)
        {
            var tokenText = context.PRE_INCLUDE().GetText();
            var includePath = ExtractIncludePath(tokenText);
            if (!string.IsNullOrEmpty(includePath))
            {
                Includes.Add(includePath);
            }
        }

        return base.VisitDirective(context);
    }

    private void ResolveHierarchy()
    {
        // Re-scan class/struct/interface declarations to wire ParentSymbol/Children.
        foreach (var symbol in Symbols.Where(s =>
            s.SymbolType == SymbolType.Class ||
            s.SymbolType == SymbolType.Struct ||
            s.SymbolType == SymbolType.Interface))
        {
            if (string.IsNullOrEmpty(symbol.Detail))
                continue;

            var parentName = ExtractParentName(symbol.Detail);
            if (!string.IsNullOrEmpty(parentName) && _symbolsByName.TryGetValue(parentName, out var parent))
            {
                symbol.ParentSymbol = parent;
                if (!parent.Children.Contains(symbol))
                {
                    parent.Children.Add(symbol);
                }
            }
        }
    }

    private static string? ExtractParentName(string detail)
    {
        var colonIndex = detail.IndexOf(':');
        if (colonIndex < 0)
            return null;

        var afterColon = detail.Substring(colonIndex + 1).Trim();
        var parts = afterColon.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[parts.Length - 1] : null;
    }

    private MqlSymbol CreateTypeSymbol(string name, IToken nameToken, SymbolType symbolType)
    {
        var range = CreateRangeFromToken(nameToken);
        return new MqlSymbol
        {
            Name = name,
            Kind = symbolType.ToLspSymbolKind(),
            SymbolType = symbolType,
            Range = range,
            SelectionRange = range,
            Detail = symbolType.ToString().ToLowerInvariant() + " " + name,
            FilePath = _filePath
        };
    }

    private string? ExtractIncludePath(string tokenText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(tokenText, @"#include\s+""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = System.Text.RegularExpressions.Regex.Match(tokenText, @"#include\s+\u003c([^\u003e]+)\u003e");
        if (match.Success)
        {
            return $"<{match.Groups[1].Value}>";
        }

        return null;
    }

    private static LspRange CreateRangeFromToken(IToken token)
    {
        return new LspRange
        (
            new LspPosition(token.Line - 1, token.Column),
            new LspPosition(token.Line - 1, token.Column + token.Text.Length)
        );
    }

    private static LspRange CreateFullFunctionRange(Mql5GrammarParser.FunctionDeclarationContext context, IToken nameToken)
    {
        var startLine = nameToken.Line - 1;
        var startColumn = nameToken.Column;

        var blockContext = context.block();
        if (blockContext != null && blockContext.RBRACE() != null)
        {
            var rbrace = blockContext.RBRACE().Symbol;
            return new LspRange(
                new LspPosition(startLine, startColumn),
                new LspPosition(rbrace.Line - 1, rbrace.Column + rbrace.Text.Length)
            );
        }

        return new LspRange(
            new LspPosition(startLine, startColumn),
            new LspPosition(context.Stop.Line - 1, context.Stop.Column + context.Stop.Text.Length)
        );
    }
}
