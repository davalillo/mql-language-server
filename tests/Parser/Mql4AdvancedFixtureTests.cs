using System.IO;
using System.Linq;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace MqlLanguageServer.Tests.Parser;

/// <summary>
/// TDD-driven integration tests for advanced MQL4 grammar constructs.
/// Each .mq4 fixture under tests/fixtures/mq4/advanced must parse through
/// Mql4AntlrParser without throwing. Per-fixture Fact tests assert specific
/// symbols are extracted (functions and variables only — the Mql4SymbolVisitor
/// does NOT extract classes, structs, or enums).
/// </summary>
public class Mql4AdvancedFixtureTests
{
    private static readonly string FixturesDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(Mql4AdvancedFixtureTests).Assembly.Location)!,
        "..", "..", "..", "..", "tests", "fixtures", "mq4", "advanced");

    private static string GetFixturePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(FixturesDirectory, fileName));
    }

    private static Mql4File ParseFixture(string fileName)
    {
        var path = GetFixturePath(fileName);
        Assert.True(File.Exists(path), $"Fixture not found: {path}");

        var parser = new Mql4AntlrParser();
        // ParseFileFromPath calls ParseFile internally; ParseFile catches exceptions
        // and returns a non-null Mql4File (possibly with empty symbols on parse error).
        var file = parser.ParseFileFromPath(path);

        Assert.NotNull(file);
        // A clean fixture must not produce any ANTLR syntax errors.
        Assert.Empty(file.SyntaxErrors);
        return file;
    }

    /// <summary>
    /// Asserts that a symbol with the given name exists in the parsed file.
    /// Optionally checks its LSP SymbolKind.
    /// </summary>
    private static void AssertHasSymbol(Mql4File file, string name, SymbolKind? expectedKind = null)
    {
        var symbol = file.Symbols.FirstOrDefault(s => s.Name == name);
        Assert.NotNull(symbol);
        if (expectedKind.HasValue)
        {
            Assert.True(symbol!.Kind == expectedKind.Value,
                $"Symbol '{name}' expected Kind={expectedKind.Value}, got {symbol.Kind}");
        }
    }

    [Theory]
    [InlineData("advanced_templates.mq4")]
    [InlineData("advanced_class_inheritance.mq4")]
    [InlineData("advanced_constructor_destructor.mq4")]
    [InlineData("advanced_global_ctor_dtor.mq4")]
    [InlineData("advanced_enum_values.mq4")]
    [InlineData("advanced_operator_overload.mq4")]
    [InlineData("advanced_import.mq4")]
    [InlineData("advanced_preprocessor.mq4")]
    [InlineData("advanced_small_types.mq4")]
    [InlineData("advanced_multidim_arrays.mq4")]
    [InlineData("advanced_references.mq4")]
    [InlineData("advanced_new_delete_sizeof.mq4")]
    [InlineData("advanced_special_literals.mq4")]
    [InlineData("advanced_modifiers.mq4")]
    [InlineData("advanced_control_flow.mq4")]
    [InlineData("advanced_array_initializer.mq4")]
    public void Fixture_Parses_WithoutErrors(string fileName)
    {
        ParseFixture(fileName);
    }

    [Fact]
    public void Templates_Extracts_MaxValue_OnTick()
    {
        var file = ParseFixture("advanced_templates.mq4");

        Assert.True(file.Symbols.Count > 0, "Should extract symbols from advanced_templates");
        AssertHasSymbol(file, "MaxValue", SymbolKind.Function);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void ClassInheritance_Extracts_OnTick()
    {
        var file = ParseFixture("advanced_class_inheritance.mq4");

        // The visitor does NOT extract class symbols, so we only assert functions.
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void ConstructorDestructor_Extracts_OnTick()
    {
        var file = ParseFixture("advanced_constructor_destructor.mq4");

        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void GlobalCtorDtor_ParsesWithoutErrors()
    {
        var file = ParseFixture("advanced_global_ctor_dtor.mq4");

        // globalConstructorDeclaration and globalDestructorDeclaration are parsed
        // by the grammar, but the Mql4SymbolVisitor has NO visitor overrides for them
        // (only VisitFunctionDeclaration and VisitVariableDeclaration extract symbols).
        // So the file parses cleanly but may have zero symbols extracted.
        // We assert the file is non-null (parsing did not throw).
        Assert.NotNull(file);
    }

    [Fact]
    public void EnumValues_Extracts_OnTick()
    {
        var file = ParseFixture("advanced_enum_values.mq4");

        // The visitor does NOT extract enum symbols, so we only assert functions.
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void OperatorOverload_Extracts_AddOperator_OnTick()
    {
        var file = ParseFixture("advanced_operator_overload.mq4");

        // MQL4 grammar does NOT support operator overloading (no rule consumes K_OPERATOR).
        // The fixture uses a plain function AddOperator instead.
        AssertHasSymbol(file, "AddOperator", SymbolKind.Function);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void Import_Extracts_MessageBoxA_OnTick()
    {
        var file = ParseFixture("advanced_import.mq4");

        // #import is recognized by VisitDirective but NOT added to Includes.
        // Only #include populates the Includes list.
        AssertHasSymbol(file, "MessageBoxA", SymbolKind.Function);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
        // Includes should be empty (only #import is present, not #include)
        Assert.Empty(file.Includes);
    }

    [Fact]
    public void Preprocessor_Extracts_Macros_AndSymbols()
    {
        var file = ParseFixture("advanced_preprocessor.mq4");

        // Macros are extracted via token scanning (PRE_DEFINE on Channel 1).
        Assert.NotEmpty(file.Macros);
        Assert.Contains(file.Macros, m => m == "DEBUG_VERSION");
        Assert.Contains(file.Macros, m => m == "MAX_BARS");

        // Variables and functions should also be extracted
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void SmallTypes_Extracts_Variables_AndFunction()
    {
        var file = ParseFixture("advanced_small_types.mq4");

        AssertHasSymbol(file, "c", SymbolKind.Variable);
        AssertHasSymbol(file, "uc", SymbolKind.Variable);
        AssertHasSymbol(file, "s", SymbolKind.Variable);
        AssertHasSymbol(file, "us", SymbolKind.Variable);
        AssertHasSymbol(file, "ui", SymbolKind.Variable);
        AssertHasSymbol(file, "l", SymbolKind.Variable);
        AssertHasSymbol(file, "ul", SymbolKind.Variable);
        AssertHasSymbol(file, "f", SymbolKind.Variable);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void MultidimArrays_Extracts_Variables_AndFunctions()
    {
        var file = ParseFixture("advanced_multidim_arrays.mq4");

        AssertHasSymbol(file, "matrix", SymbolKind.Variable);
        AssertHasSymbol(file, "cube", SymbolKind.Variable);
        AssertHasSymbol(file, "simple", SymbolKind.Variable);
        AssertHasSymbol(file, "FillMatrix", SymbolKind.Function);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void References_Extracts_Functions()
    {
        var file = ParseFixture("advanced_references.mq4");

        AssertHasSymbol(file, "IncrementBy", SymbolKind.Function);
        AssertHasSymbol(file, "Swap", SymbolKind.Function);
        AssertHasSymbol(file, "GetOut", SymbolKind.Function);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void NewDeleteSizeof_Extracts_OnTick()
    {
        var file = ParseFixture("advanced_new_delete_sizeof.mq4");

        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void SpecialLiterals_Extracts_Variables_AndFunction()
    {
        var file = ParseFixture("advanced_special_literals.mq4");

        AssertHasSymbol(file, "startDate", SymbolKind.Variable);
        AssertHasSymbol(file, "red", SymbolKind.Variable);
        AssertHasSymbol(file, "green", SymbolKind.Variable);
        AssertHasSymbol(file, "hexValue", SymbolKind.Variable);
        AssertHasSymbol(file, "letter", SymbolKind.Variable);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void Modifiers_Extracts_InputAndStaticVariables()
    {
        var file = ParseFixture("advanced_modifiers.mq4");

        AssertHasSymbol(file, "InpPeriod", SymbolKind.Variable);
        AssertHasSymbol(file, "InpLotSize", SymbolKind.Variable);
        AssertHasSymbol(file, "InpHidden", SymbolKind.Variable);
        AssertHasSymbol(file, "ExtFlag", SymbolKind.Variable);
        AssertHasSymbol(file, "StaticCounter", SymbolKind.Variable);
        AssertHasSymbol(file, "Pi", SymbolKind.Variable);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void ControlFlow_Extracts_OnTick()
    {
        var file = ParseFixture("advanced_control_flow.mq4");

        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }

    [Fact]
    public void ArrayInitializer_Extracts_Variables_AndFunction()
    {
        var file = ParseFixture("advanced_array_initializer.mq4");

        AssertHasSymbol(file, "values", SymbolKind.Variable);
        AssertHasSymbol(file, "sparse", SymbolKind.Variable);
        AssertHasSymbol(file, "nested", SymbolKind.Variable);
        AssertHasSymbol(file, "OnTick", SymbolKind.Function);
    }
}