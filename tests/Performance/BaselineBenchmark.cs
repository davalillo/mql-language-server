using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MqlLanguageServer.Tests.Performance;

/// <summary>
/// Baseline Benchmark for measuring MQL4 LSP performance
/// Implements reproducible benchmarks with warm-up runs and median calculation
/// </summary>
public class BaselineBenchmark
{
    public BaselineBenchmark()
    {
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
