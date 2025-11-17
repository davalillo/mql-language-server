// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using Mql4LanguageServer.Parser;
using Mql4LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

Console.WriteLine("=== MQL4 Language Server ===\n");

// Test the parser
var parser = new Mql4Parser();
var content = File.ReadAllText("test_parser.mq4");
var file = parser.ParseFile("test_parser.mq4", content);

Console.WriteLine($"File: {file.FilePath}");
Console.WriteLine($"Symbols found: {file.Symbols.Count}");
Console.WriteLine($"Includes: {file.Includes.Count}\n");

Console.WriteLine("=== Symbols ===");
foreach (var symbol in file.Symbols)
{
    Console.WriteLine($"- {symbol.Name} (Kind: {symbol.Kind}, Predefined: {symbol.IsPredefined})");
}

Console.WriteLine("\n=== Includes ===");
foreach (var include in file.Includes)
{
    Console.WriteLine($"- {include}");
}

Console.WriteLine("\n=== Testing FindSymbolAtPosition ===");
var testPosition = new Position(5, 5); // Línea 6, columna 6 (en OnInit)
var symbolAtPos = parser.FindSymbolAtPosition(file, testPosition);
if (symbolAtPos != null)
{
    Console.WriteLine($"Found symbol at position: {symbolAtPos.Name}");
}
else
{
    Console.WriteLine("No symbol found at position");
}

Console.WriteLine("\n=== Test Complete ===");
