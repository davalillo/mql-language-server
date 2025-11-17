using System;
using System.IO;
using System.Linq;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;

// Test harness for MQL4 ANTLR Parser
Console.WriteLine("=== MQL4 ANTLR Parser Test ===");
Console.WriteLine();

// Parse test file
var parser = new Mql4AntlrParser();
var testFilePath = "/home/guillermo/source/mql4-language-server/test_parser.mq4";

Console.WriteLine($"Test file path: {testFilePath}");
Console.WriteLine();

try
{
    var mql4File = parser.ParseFileFromPath(testFilePath);

    Console.WriteLine($"Parsed file: {mql4File.FilePath}");
    Console.WriteLine($"Symbols found: {mql4File.Symbols.Count}");
    Console.WriteLine($"Includes found: {mql4File.Includes.Count}");
    Console.WriteLine();

    // Display symbols
    Console.WriteLine("--- Symbols ---");
    foreach(var symbol in mql4File.Symbols)
    {
        Console.WriteLine($"  {symbol.Name} ({symbol.Kind})");
        Console.WriteLine($"    Range: Line {symbol.Range.Start.Line}-{symbol.Range.End.Line}");
        Console.WriteLine($"    Detail: {symbol.Detail}");
    }
    Console.WriteLine();

    // Display includes
    Console.WriteLine("--- Includes ---");
    foreach(var include in mql4File.Includes)
    {
        Console.WriteLine($"  {include}");
    }
    Console.WriteLine();

    // Test FindSymbolAtPosition
    Console.WriteLine("--- Testing FindSymbolAtPosition ---");
    var testPos = parser.FindSymbolAtPosition(10, 5); // Line 10, column 5
    if(testPos != null)
    {
        Console.WriteLine($"Found symbol at position: {testPos.Name}");
    }
    else
    {
        Console.WriteLine("No symbol found at position");
    }
    Console.WriteLine();

    // Test completions
    Console.WriteLine("--- Testing Completions ---");
    var completions = parser.GetCompletions(5, 1);
    Console.WriteLine($"Total completions available: {completions.Count()}");
    Console.WriteLine($"First 10 completions: {string.Join(", ", completions.Take(10))}");
    Console.WriteLine();

    Console.WriteLine("=== Test completed successfully ===");
}
catch(Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
}
