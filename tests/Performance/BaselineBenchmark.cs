using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using MqlLanguageServer.Parser;

namespace MqlLanguageServer.Tests.Performance;

/// <summary>
/// Baseline Benchmark for measuring MQL4 LSP performance
/// Implements reproducible benchmarks with warm-up runs and median calculation
/// </summary>
public class BaselineBenchmark
{
    private readonly Mql4AntlrParser _parser;
    private readonly string _testFilePath;
    private readonly string _testFileContent;
    private const int WarmupRuns = 10;
    private const int BenchmarkRuns = 20;

    public BaselineBenchmark()
    {
        _parser = new Mql4AntlrParser();
        _testFilePath = GetFixtureFilePath("Ducibus_Pro_ver_2_90.mq4");
        _testFileContent = File.ReadAllText(_testFilePath);
    }

    #region Environment Detection

    /// <summary>
    /// Detects the current power mode (battery vs plugged in)
    /// </summary>
    private string DetectPowerMode()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Check power status via WMI
                return IsWindowsOnBattery() ? "Battery" : "PluggedIn";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Check /sys/class/power_supply/
                return IsLinuxOnBattery() ? "Battery" : "PluggedIn";
            }
            else
            {
                // macOS or unknown: assume plugged in
                return "Unknown";
            }
        }
        catch
        {
            return "Unknown";
        }
    }

    private bool IsWindowsOnBattery()
    {
        try
        {
            var output = ExecuteCommand("wmic", "path Win32_PowerManagementEvent get EventType /value");
            return output.Contains("EventType=4"); // 4 = Battery depleted/low
        }
        catch
        {
            return false; // Assume plugged in on error
        }
    }

    private bool IsLinuxOnBattery()
    {
        try
        {
            if (File.Exists("/sys/class/power_supply/BAT0/status"))
            {
                var status = File.ReadAllText("/sys/class/power_supply/BAT0/status").Trim().ToLower();
                return status.Contains("discharging") || status.Contains("low");
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string ExecuteCommand(string command, string args)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Detects the current environment information
    /// </summary>
    private BenchmarkEnvironment DetectEnvironment()
    {
        return new BenchmarkEnvironment
        {
            PowerMode = DetectPowerMode(),
            OS = $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}",
            DotnetVersion = Environment.Version.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            MachineName = Environment.MachineName,
            Timestamp = DateTime.UtcNow
        };
    }

    #endregion

    #region Benchmark Methods

    /// <summary>
    /// Main benchmark method that measures all operations
    /// Only runs if ENABLE_BENCHMARK=true environment variable is set
    /// </summary>
    [SkippableFact]
    public async Task Baseline_BenchmarkAllOperationsAsync()
    {
        Skip.IfNot(ShouldRunBenchmark(), "Benchmark disabled. Set ENABLE_BENCHMARK=true to run.");

        var env = DetectEnvironment();
        LogEnvironment(env);

        // Warm-up phase: execute operations without measuring
        Console.WriteLine("\n=== WARM-UP PHASE ===");
        Console.WriteLine($"Running {WarmupRuns} warm-up iterations...");
        for (int i = 0; i < WarmupRuns; i++)
        {
            _parser.ParseFile(_testFileContent, _testFilePath);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(10); // Small delay between runs
        }
        Console.WriteLine("Warm-up complete.\n");

        // Benchmark phase: measure performance
        Console.WriteLine("=== BENCHMARK PHASE ===");
        Console.WriteLine($"Running {BenchmarkRuns} benchmark iterations...\n");

        var parsingTimes = new List<double>();
        var symbolCounts = new List<int>();
        var completionCounts = new List<int>();

        for (int i = 0; i < BenchmarkRuns; i++)
        {
            // Measure parsing
            var sw = Stopwatch.StartNew();
            var file = _parser.ParseFile(_testFileContent, _testFilePath);
            sw.Stop();

            parsingTimes.Add(sw.Elapsed.TotalMilliseconds);
            symbolCounts.Add(file.Symbols.Count);

            // Measure completion
            // Measure completion
            var completions = _parser.GetCompletions(file, 1, 1).ToList();
            completionCounts.Add(completions.Count);
            // Force GC between iterations
            if (i < BenchmarkRuns - 1)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(10);
            }

            Console.WriteLine($"  Iteration {i + 1}/{BenchmarkRuns}: {sw.Elapsed.TotalMilliseconds:F2}ms, {file.Symbols.Count} symbols");
        }

        Console.WriteLine("\n=== RESULTS ===");

        // Calculate statistics (using median for robustness)
        var results = new BenchmarkResults
        {
            Environment = env,
            Parsing = CalculateStats(parsingTimes),
            SymbolCount = CalculateStats(symbolCounts.Select(x => (double)x).ToList()),
            CompletionCount = CalculateStats(completionCounts.Select(x => (double)x).ToList()),
            CommitHash = GetCurrentGitCommit()!
        };

        PrintResults(results);

        // Save results
        var milestoneName = Environment.GetEnvironmentVariable("BENCHMARK_MILESTONE") ?? "baseline";
        SaveResults(results, milestoneName);

        // Verify basic expectations
        Assert.True(results.Parsing.Median > 0, "Parsing time should be > 0");
        Assert.True(results.Parsing.Median < 10000, "Parsing time should be < 10 seconds");
        Assert.True(results.SymbolCount.Median > 0, "Should parse at least 1 symbol");
        Assert.True(results.CompletionCount.Median > 100, "Should have > 100 completions (builtins + symbols)");

        Console.WriteLine($"\n✅ Benchmark complete. Results saved to benchmarks/{milestoneName}.json");
    }

    /// <summary>
    /// Checks if the benchmark should run based on environment variables
    /// </summary>
    private bool ShouldRunBenchmark()
    {
        var envVar = Environment.GetEnvironmentVariable("ENABLE_BENCHMARK");
        if (envVar?.ToLower() == "true") return true;

        // Auto-detect CI/CD
        if (IsContinuousIntegration()) return true;

        return false;
    }

    private bool IsContinuousIntegration()
    {
        return Environment.GetEnvironmentVariable("CI") != null ||
               Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null ||
               Environment.GetEnvironmentVariable("TEAMCITY_VERSION") != null;
    }

    #endregion

    #region Statistics and Utilities

    private BenchmarkStats CalculateStats(List<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;

        return new BenchmarkStats
        {
            Median = GetMedian(sorted),
            Average = values.Average(),
            Min = values.Min(),
            Max = values.Max(),
            Samples = count
        };
    }

    private double GetMedian(List<double> sortedValues)
    {
        var count = sortedValues.Count;
        if (count % 2 == 0)
        {
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
        }
        else
        {
            return sortedValues[count / 2];
        }
    }

    private void LogEnvironment(BenchmarkEnvironment env)
    {
        Console.WriteLine("=== ENVIRONMENT ===");
        Console.WriteLine($"Power Mode: {env.PowerMode}");
        Console.WriteLine($"OS: {env.OS}");
        Console.WriteLine($".NET: {env.DotnetVersion}");
        Console.WriteLine($"Processors: {env.ProcessorCount}");
        Console.WriteLine($"Machine: {env.MachineName}");
        Console.WriteLine($"Timestamp: {env.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();

        if (env.PowerMode == "Battery")
        {
            Console.WriteLine("⚠️  WARNING: Running on battery. Results may be inaccurate.");
            Console.WriteLine("           For reliable benchmarks, plug in power and re-run.");
            Console.WriteLine();
        }
        else if (env.PowerMode == "Unknown")
        {
            Console.WriteLine("⚠️  WARNING: Could not detect power mode. Results may vary.");
            Console.WriteLine();
        }
    }

    private void PrintResults(BenchmarkResults results)
    {
        Console.WriteLine("\n=== BENCHMARK RESULTS ===");
        Console.WriteLine($"Parsing Performance:");
        Console.WriteLine($"  Median: {results.Parsing.Median:F2}ms");
        Console.WriteLine($"  Average: {results.Parsing.Average:F2}ms");
        Console.WriteLine($"  Range: {results.Parsing.Min:F2}ms - {results.Parsing.Max:F2}ms");
        Console.WriteLine($"  Samples: {results.Parsing.Samples}");

        Console.WriteLine($"\nSymbols Parsed:");
        Console.WriteLine($"  Median: {results.SymbolCount.Median:F0}");
        Console.WriteLine($"  Range: {results.SymbolCount.Min:F0} - {results.SymbolCount.Max:F0}");

        Console.WriteLine($"\nCompletions Available:");
        Console.WriteLine($"  Median: {results.CompletionCount.Median:F0}");
        Console.WriteLine($"  Range: {results.CompletionCount.Min:F0} - {results.CompletionCount.Max:F0}");

        Console.WriteLine($"\nGit Commit: {results.CommitHash ?? "unknown"}");
    }

    private string? GetCurrentGitCommit()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(output) ? "unknown" : output;
        }
        catch
        {
            return "unknown";
        }
    }

    private void SaveResults(BenchmarkResults results, string milestoneName)
    {
        // Create benchmarks directory if it doesn't exist
        var benchmarksDir = "benchmarks";
        if (!Directory.Exists(benchmarksDir))
        {
            Directory.CreateDirectory(benchmarksDir);
        }

        // Save JSON file
        var filePath = Path.Combine(benchmarksDir, $"{milestoneName}.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(results, options);
        File.WriteAllText(filePath, json);

        // Also print the JSON to console
        Console.WriteLine($"\n=== JSON OUTPUT ({milestoneName}.json) ===");
        Console.WriteLine(json);
    }

    private string GetFixtureFilePath(string fileName)
    {
        var projectRoot = GetProjectRoot();
        return Path.Combine(projectRoot, "tests", "fixtures", "real", fileName);
    }

    private string GetProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir, "MqlLanguageServer.sln")))
            {
                return currentDir;
            }
            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    #endregion

    #region Data Models

    public class BenchmarkEnvironment
    {
        public required string PowerMode { get; set; }
        public required string OS { get; set; }
        public required string DotnetVersion { get; set; }
        public int ProcessorCount { get; set; }
        public required string MachineName { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BenchmarkStats
    {
        public double Median { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public int Samples { get; set; }
    }

    public class BenchmarkResults
    {
        public required BenchmarkEnvironment Environment { get; set; }
        public required BenchmarkStats Parsing { get; set; }
        public required BenchmarkStats SymbolCount { get; set; }
        public required BenchmarkStats CompletionCount { get; set; }
        public string CommitHash { get; set; } = "unknown";
    }

    #endregion
}
