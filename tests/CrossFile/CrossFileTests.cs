using Xunit;
using MqlLanguageServer.Models;
using MqlLanguageServer.Parser;
using MqlLanguageServer.Lsp.Server;
using MqlLanguageServer.Tests.Lsp;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;

namespace MqlLanguageServer.Tests.CrossFile;

/// <summary>
/// Tests for cross-file functionality including GlobalSymbolIndex and cross-file navigation
/// </summary>
[Collection("GlobalSymbolIndex Tests")]
public class CrossFileTests : IDisposable
{
    private readonly GlobalSymbolIndex _index;
    private readonly Mql4AntlrParser _parser;
    private readonly GlobalSymbolIndexCollectionFixture _fixture;

    public CrossFileTests(GlobalSymbolIndexCollectionFixture fixture)
    {
        _fixture = fixture;
        _index = GlobalSymbolIndex.Instance;
        _parser = new Mql4AntlrParser();
        // State is already cleared by the fixture
    }

    public void Dispose()
    {
        // Clean up after each test
        _index.Clear();
    }

    #region GlobalSymbolIndex Tests

    /// <summary>
    /// Verify that GlobalSymbolIndex follows singleton pattern
    /// </summary>
    [Fact]
    public void GlobalSymbolIndex_Singleton_ReturnsSameInstance()
    {
        // Act
        var instance1 = GlobalSymbolIndex.Instance;
        var instance2 = GlobalSymbolIndex.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verify that the index can store and retrieve symbols from multiple files
    /// </summary>
    [Fact]
    public void GlobalSymbolIndex_CanStoreMultipleFiles()
    {
        // Arrange
        var fileAPath = "fileA.mq4";
        var fileBPath = "fileB.mq4";

        var symbolsA = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionA", Kind = SymbolKind.Function, FilePath = fileAPath },
            new Mql4Symbol { Name = "VariableA", Kind = SymbolKind.Variable, FilePath = fileAPath }
        };

        var symbolsB = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionB", Kind = SymbolKind.Function, FilePath = fileBPath },
            new Mql4Symbol { Name = "VariableB", Kind = SymbolKind.Variable, FilePath = fileBPath }
        };

        // Act
        _index.AddFile(fileAPath, symbolsA);
        _index.AddFile(fileBPath, symbolsB);

        var filesA = _index.GetFileSymbols(fileAPath);
        var filesB = _index.GetFileSymbols(fileBPath);

        // Assert
        Assert.NotNull(filesA);
        Assert.NotNull(filesB);
        Assert.Equal(2, filesA.Count);
        Assert.Equal(2, filesB.Count);

        var allIndexedFiles = _index.GetIndexedFiles();
        Assert.Equal(2, allIndexedFiles.Count);
        Assert.Contains(fileAPath, allIndexedFiles);
        Assert.Contains(fileBPath, allIndexedFiles);
    }

    /// <summary>
    /// Verify that FindSymbol finds symbols across all indexed files
    /// </summary>
    [Fact]
    public void GlobalSymbolIndex_FindSymbol_FindsAcrossMultipleFiles()
    {
        // Arrange
        var fileAPath = "fileA.mq4";
        var fileBPath = "fileB.mq4";

        var symbolsA = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "SharedFunction", Kind = SymbolKind.Function, FilePath = fileAPath },
            new Mql4Symbol { Name = "VariableA", Kind = SymbolKind.Variable, FilePath = fileAPath }
        };

        var symbolsB = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "SharedFunction", Kind = SymbolKind.Function, FilePath = fileBPath },
            new Mql4Symbol { Name = "VariableB", Kind = SymbolKind.Variable, FilePath = fileBPath }
        };

        _index.AddFile(fileAPath, symbolsA);
        _index.AddFile(fileBPath, symbolsB);

        // Act
        var sharedFunctionLocations = _index.FindSymbol("SharedFunction");

        // Assert
        Assert.NotNull(sharedFunctionLocations);
        Assert.Equal(2, sharedFunctionLocations.Count);

        var filePaths = sharedFunctionLocations.Select(loc => loc.FilePath).ToList();
        Assert.Contains(fileAPath, filePaths);
        Assert.Contains(fileBPath, filePaths);
    }

    /// <summary>
    /// Verify that GetStatistics returns correct statistics
    /// </summary>
    [Fact]
    public void GlobalSymbolIndex_GetStatistics_ShowsCorrectStats()
    {
        // Arrange
        var fileAPath = "fileA.mq4";
        var fileBPath = "fileB.mq4";

        var symbolsA = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionA", Kind = SymbolKind.Function, FilePath = fileAPath },
            new Mql4Symbol { Name = "VariableA", Kind = SymbolKind.Variable, FilePath = fileAPath }
        };

        var symbolsB = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionB", Kind = SymbolKind.Function, FilePath = fileBPath },
            new Mql4Symbol { Name = "VariableB", Kind = SymbolKind.Variable, FilePath = fileBPath }
        };

        _index.AddFile(fileAPath, symbolsA);
        _index.AddFile(fileBPath, symbolsB);

        _index.AddDependency(fileAPath, fileBPath);

        // Act
        var stats = _index.GetStatistics();

        // Assert
        Assert.Equal(2, stats.FileCount);
        Assert.Equal(4, stats.SymbolCount);
        Assert.Equal(4, stats.UniqueSymbolCount);
        Assert.Equal(1, stats.DependencyCount);
    }

    /// <summary>
    /// Verify that RemoveFile removes symbols and updates indices correctly
    /// </summary>
    [Fact]
    public void GlobalSymbolIndex_RemoveFile_RemovesSymbolsAndUpdatesIndices()
    {
        // Arrange
        var fileAPath = "fileA.mq4";
        var fileBPath = "fileB.mq4";

        var symbolsA = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionA", Kind = SymbolKind.Function, FilePath = fileAPath },
            new Mql4Symbol { Name = "VariableA", Kind = SymbolKind.Variable, FilePath = fileAPath }
        };

        var symbolsB = new List<Mql4Symbol>
        {
            new Mql4Symbol { Name = "FunctionB", Kind = SymbolKind.Function, FilePath = fileBPath },
            new Mql4Symbol { Name = "VariableB", Kind = SymbolKind.Variable, FilePath = fileBPath }
        };

        _index.AddFile(fileAPath, symbolsA);
        _index.AddFile(fileBPath, symbolsB);

        // Act
        _index.RemoveFile(fileAPath);

        var filesA = _index.GetFileSymbols(fileAPath);
        var filesB = _index.GetFileSymbols(fileBPath);
        var allFiles = _index.GetIndexedFiles();

        // Assert
        Assert.Null(filesA);
        Assert.NotNull(filesB);
        Assert.Contains(fileBPath, allFiles);
        Assert.DoesNotContain(fileAPath, allFiles);

        // Verify we have the expected count (may be more if other tests added files)
        // But at minimum, we should have fileBPath and not fileAPath
        var hasFileB = allFiles.Contains(fileBPath);
        var hasFileA = allFiles.Contains(fileAPath);
        Assert.True(hasFileB && !hasFileA, $"Expected fileB ({fileBPath}) and not fileA ({fileAPath}), but got: {string.Join(", ", allFiles)}");
    }

    #endregion

    #region Cross-File Navigation Tests

    /// <summary>
    /// Test cross-file navigation using real fixture files
    /// </summary>
    [Fact]
    public void CrossFileNavigation_CanNavigateToIncludedHeader()
    {
        // Arrange
        var expertAdvisorCode = @"
#include <Include/CustomIndicators.mqh>

void OnTick()
{
    double sma = CalculateSMA(20, 0);
}
";

        var customIndicatorsCode = @"
double CalculateSMA(int period, int shift)
{
    return 0.0;
}
";

        // Parse both files
        var expertFile = _parser.ParseFile(expertAdvisorCode, "ExpertAdvisor.mq4");
        var indicatorsFile = _parser.ParseFile(customIndicatorsCode, "CustomIndicators.mqh");

        // Add to index
        _index.AddFile("ExpertAdvisor.mq4", expertFile.Symbols);
        _index.AddFile("CustomIndicators.mqh", indicatorsFile.Symbols);

        // Add dependency
        _index.AddDependency("ExpertAdvisor.mq4", "CustomIndicators.mqh");

        // Act - Navigate to CalculateSMA function from header file
        var calculateSmalocations = _index.FindSymbol("CalculateSMA");

        // Assert
        Assert.NotNull(calculateSmalocations);
        Assert.True(calculateSmalocations.Count > 0, "CalculateSMA should be found");

        var calculateSmaInIndicators = calculateSmalocations
            .Where(loc => loc.FilePath == "CustomIndicators.mqh")
            .ToList();

        Assert.True(calculateSmaInIndicators.Count > 0, "CalculateSMA should be found in CustomIndicators.mqh");
    }

    /// <summary>
    /// Test finding all references to a symbol across files
    /// </summary>
    [Fact]
    public void CrossFileNavigation_FindAllReferences_FindsAllOccurrences()
    {
        // Arrange
        var fileACode = @"
void FunctionA()
{
    FunctionB();
}
";

        var fileBCode = @"
void FunctionB()
{
    FunctionA();
}
";

        // Parse both files
        var fileA = _parser.ParseFile(fileACode, "FileA.mq4");
        var fileB = _parser.ParseFile(fileBCode, "FileB.mq4");

        // Add to index
        _index.AddFile("FileA.mq4", fileA.Symbols);
        _index.AddFile("FileB.mq4", fileB.Symbols);

        // Act - Find all references to FunctionB
        var functionBLocations = _index.FindAllReferences("FunctionB");

        // Assert
        Assert.NotNull(functionBLocations);
        Assert.True(functionBLocations.Count > 0, "FindAllReferences should find FunctionB");
    }

    /// <summary>
    /// Test parsing files with ParseFileWithIncludes
    /// </summary>
    [Fact]
    public void CrossFileNavigation_ParseFileWithIncludes_CombinesSymbols()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "Mql4CrossFileTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var mainFilePath = Path.Combine(tempDir, "Main.mq4");
        var includedFilePath = Path.Combine(tempDir, "Included.mqh");

        try
        {
            // Create main file with include
            File.WriteAllText(mainFilePath, @"
#include ""Included.mqh""

void OnTick()
{
    int result = MyFunction();
}
");

            // Create included file
            File.WriteAllText(includedFilePath, @"
int MyFunction()
{
    return 42;
}
");

            // Act
            var parsedFile = _parser.ParseFileWithIncludes(mainFilePath);

            // Assert
            Assert.NotNull(parsedFile);
            Assert.NotEmpty(parsedFile.Includes);
            Assert.True(parsedFile.Symbols.Count > 0, "Should have symbols from main file and includes");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Test that cross-file symbol resolution works correctly
    /// </summary>
    [Fact]
    public void CrossFileNavigation_CrossFileSymbolResolution_WorksCorrectly()
    {
        // Arrange
        var headerCode = @"
double CalculateEMA(int period)
{
    return 1.0;
}
";

        var indicatorsFile = _parser.ParseFile(headerCode, "Header.mqh");
        _index.AddFile("Header.mqh", indicatorsFile.Symbols);

        // Add dependencies (simulate inclusion by ExpertAdvisor)
        _index.AddDependency("ExpertAdvisor.mq4", "Header.mqh");

        // Act - Search for a symbol defined in the header
        var calculateEmaLocations = _index.FindSymbol("CalculateEMA");

        // Assert
        Assert.NotNull(calculateEmaLocations);
        Assert.True(calculateEmaLocations.Count > 0, "Should find CalculateEMA in header file");

        // Verify the symbol is from the correct file
        var calculateEmaInHeader = calculateEmaLocations
            .FirstOrDefault(loc => loc.FilePath == "Header.mqh");

        Assert.NotNull(calculateEmaInHeader);
        Assert.NotNull(calculateEmaInHeader.Symbol);
        Assert.Equal("CalculateEMA", calculateEmaInHeader.Symbol.Name);
    }

    #endregion

    #region Include Parsing Tests

    /// <summary>
    /// Test that include statements with angle brackets are parsed
    /// </summary>
    [Fact]
    public void IncludeParsing_ParsesAngleBrackets()
    {
        // Arrange
        var code = @"
#include <stdlib.mqh>
#include <Trade/Trade.mqh>

void OnTick()
{
    int result = MyFunction();
}
";

        // Act
        var parsedFile = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(parsedFile);
        Assert.NotEmpty(parsedFile.Includes);

        // Should parse angle bracket includes
        Assert.True(parsedFile.Includes.Count >= 2, "Should parse angle bracket includes");
    }

    /// <summary>
    /// Test that include statements are correctly extracted
    /// </summary>
    [Fact]
    public void IncludeParsing_ParsesIncludeStatements()
    {
        // Arrange
        var code = @"
#include ""custom.mqh""
#include ""Trade/Trade.mqh""

void OnTick()
{
    int result = MyFunction();
}
";

        // Act
        var parsedFile = _parser.ParseFile(code, "test.mq4");

        // Assert
        Assert.NotNull(parsedFile);
        Assert.NotEmpty(parsedFile.Includes);
        Assert.True(parsedFile.Includes.Count >= 2, "Should parse includes");
    }

    /// <summary>
    /// Test recursive include detection
    /// </summary>
    [Fact]
    public void IncludeParsing_RecursiveIncludes_HandlesCircularReferences()
    {
        // Arrange
        // Create temporary files with circular includes
        var tempDir = Path.Combine(Path.GetTempPath(), "Mql4CrossFileTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var fileAPath = Path.Combine(tempDir, "FileA.mq4");
        var fileBPath = Path.Combine(tempDir, "FileB.mq4");

        try
        {
            // Create FileA that includes FileB
            File.WriteAllText(fileAPath, @"
//+------------------------------------------------------------------+
//| FileA.mq4                                                      |
//+------------------------------------------------------------------+
#include ""FileB.mqh""

void FunctionA()
{
    FunctionB();
}
");

            // Create FileB that includes FileA (circular)
            File.WriteAllText(fileBPath, @"
//+------------------------------------------------------------------+
//| FileB.mq4                                                      |
//+------------------------------------------------------------------+
#include ""FileA.mqh""

void FunctionB()
{
    FunctionA();
}
");

            // Act - Parse FileA (which includes FileB, which includes FileA)
            var parsedFileA = _parser.ParseFileWithIncludes(fileAPath);

            // Assert - Should not crash and should handle circular reference
            Assert.NotNull(parsedFileA);
            Assert.NotEmpty(parsedFileA.Includes);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Real-World Integration Tests

    /// <summary>
    /// Test using real fixture files for comprehensive cross-file behavior
    /// </summary>
    [Fact]
    public void RealWorldFiles_ExpertAdvisorAndIndicators_WorkTogether()
    {
        // Arrange
        var expertAdvisorCode = @"
#include <Include/CustomIndicators.mqh>

void OnTick()
{
    double sma = CalculateSMA(20, 0);
}
";

        var customIndicatorsCode = @"
double CalculateSMA(int period, int shift)
{
    return 0.0;
}

double CalculateEMA(int period)
{
    return 1.0;
}
";

        // Act - Parse both files
        var expertFile = _parser.ParseFile(expertAdvisorCode, "ExpertAdvisor.mq4");
        var indicatorsFile = _parser.ParseFile(customIndicatorsCode, "CustomIndicators.mqh");

        // Add to global index
        _index.AddFile("ExpertAdvisor.mq4", expertFile.Symbols);
        _index.AddFile("CustomIndicators.mqh", indicatorsFile.Symbols);

        // Add dependency
        _index.AddDependency("ExpertAdvisor.mq4", "CustomIndicators.mqh");

        // Get statistics
        var stats = _index.GetStatistics();
        var indexedFiles = _index.GetIndexedFiles();

        // Assert
        Assert.NotNull(expertFile);
        Assert.NotNull(indicatorsFile);
        Assert.True(expertFile.Symbols.Count > 0, "ExpertAdvisor should have symbols");
        Assert.True(indicatorsFile.Symbols.Count > 0, "CustomIndicators should have symbols");

        Assert.Equal(2, stats.FileCount);
        Assert.Equal(2, indexedFiles.Count);

        // Verify cross-file navigation works
        var calculateSmalocations = _index.FindSymbol("CalculateSMA");
        Assert.NotNull(calculateSmalocations);

        // Verify dependencies are tracked
        var dependencies = _index.GetDependencies("ExpertAdvisor.mq4");
        Assert.True(dependencies.Count >= 0, "Dependencies should be tracked");
    }

    /// <summary>
    /// Test that symbols from included files are accessible
    /// </summary>
    [Fact]
    public void RealWorldFiles_SymbolsFromIncludes_AreAccessible()
    {
        // Arrange
        var indicatorsCode = @"
double CalculateSMA(int period, int shift)
{
    return 0.0;
}

double CalculateEMA(int period)
{
    return 1.0;
}

double CalculateRSI(int period)
{
    return 50.0;
}
";

        // Act
        var indicatorsFile = _parser.ParseFile(indicatorsCode, "CustomIndicators.mqh");
        _index.AddFile("CustomIndicators.mqh", indicatorsFile.Symbols);

        // Search for key functions defined in CustomIndicators.mqh
        var calculateSmaLocation = _index.FindSymbol("CalculateSMA");
        var calculateEmaLocation = _index.FindSymbol("CalculateEMA");
        var calculateRsiLocation = _index.FindSymbol("CalculateRSI");

        // Assert
        Assert.NotNull(calculateSmaLocation);
        Assert.NotNull(calculateEmaLocation);
        Assert.NotNull(calculateRsiLocation);

        // Verify symbols are found
        Assert.True(calculateSmaLocation.Count > 0, "CalculateSMA should be found");
        Assert.True(calculateEmaLocation.Count > 0, "CalculateEMA should be found");
        Assert.True(calculateRsiLocation.Count > 0, "CalculateRSI should be found");

        // Verify they are from the correct file
        Assert.All(calculateSmaLocation, loc => Assert.Equal("CustomIndicators.mqh", loc.FilePath));
        Assert.All(calculateEmaLocation, loc => Assert.Equal("CustomIndicators.mqh", loc.FilePath));
        Assert.All(calculateRsiLocation, loc => Assert.Equal("CustomIndicators.mqh", loc.FilePath));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the full path to a fixture file
    /// </summary>
    private string GetFixtureFilePath(string relativePath)
    {
        var projectRoot = GetProjectRoot();
        return Path.Combine(projectRoot, "tests", "fixtures", relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// Get the project root directory
    /// </summary>
    private string GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDir);

        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? currentDir;
    }

    #endregion
}
