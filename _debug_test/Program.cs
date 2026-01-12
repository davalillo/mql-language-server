using System;
using System.IO;
using System.Reflection;

var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
var fixturesPath = Path.Combine(basePath ?? "", "..", "..", "..", "..", "tests", "fixtures", "samples");
var fullPath = Path.GetFullPath(fixturesPath);
var filePath = Path.Combine(fullPath, "ExpertAdvisor.mq4");

Console.WriteLine($"Base path: {basePath}");
Console.WriteLine($"Fixtures path: {fixturesPath}");
Console.WriteLine($"Full path: {fullPath}");
Console.WriteLine($"File path: {filePath}");
Console.WriteLine($"File exists: {File.Exists(filePath)}");

if (File.Exists(filePath)) {
    var content = File.ReadAllText(filePath);
    Console.WriteLine($"File length: {content.Length} chars");
}
