using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MqlLanguageServer.Tests.Performance;

/// <summary>
/// Memory profiling tests for PASO 5.4 - Verificación Final
/// Detects memory leaks and measures memory consumption
/// </summary>
public class MemoryProfilingTests
{
    public MemoryProfilingTests()
    {
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

    /// <summary>
    /// Forces garbage collection and waits for finalizers
    /// </summary>
    private void ForceGarbageCollection()
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.Collect();
    }

    /// <summary>
    /// Gets all fixture files for stress testing
    /// </summary>
    private List<string> GetFixtureFiles()
    {
        var projectRoot = GetProjectRoot();
        var fixturesDir = Path.Combine(projectRoot, "tests", "fixtures", "real");

        if (!Directory.Exists(fixturesDir))
        {
            // Try samples directory as fallback
            var samplesDir = Path.Combine(projectRoot, "tests", "fixtures", "samples");
            if (Directory.Exists(samplesDir))
            {
                return Directory.GetFiles(samplesDir, "*.mq4", SearchOption.AllDirectories)
                    .OrderBy(x => x)
                    .ToList();
            }
            return new List<string>();
        }

        return Directory.GetFiles(fixturesDir, "*.mq4")
            .OrderBy(x => x)
            .ToList();
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

    /// <summary>
    /// Analyzes memory growth pattern to detect leaks
    /// </summary>
    private List<string> AnalyzeMemoryGrowth(List<(int iteration, long memory)> samples)
    {
        var analysis = new List<string>();

        if (samples.Count < 2)
        {
            analysis.Add("Not enough samples for analysis");
            return analysis;
        }

        // Calculate trend
        var firstMemory = samples.First().memory;
        var lastMemory = samples.Last().memory;
        var totalGrowth = lastMemory - firstMemory;
        var avgGrowthPer10Iterations = totalGrowth / (samples.Count / 10);

        analysis.Add($"Total growth: {(totalGrowth / 1024.0 / 1024.0):F2} MB");
        analysis.Add($"Average growth per 10 iterations: {(avgGrowthPer10Iterations / 1024.0 / 1024.0):F2} MB");

        // Detect linear vs exponential growth
        var earlySamples = samples.Take(samples.Count / 2).ToList();
        var lateSamples = samples.Skip(samples.Count / 2).ToList();

        if (earlySamples.Count > 1 && lateSamples.Count > 1)
        {
            var earlyAvg = earlySamples.Average(s => s.memory);
            var lateAvg = lateSamples.Average(s => s.memory);
            var growth = lateAvg - earlyAvg;

            if (growth > 1024 * 1024 * 5) // 5 MB
            {
                analysis.Add($"⚠️  Linear memory growth detected: {(growth / 1024.0 / 1024.0):F2} MB");
            }
            else
            {
                analysis.Add("✓ No significant linear growth detected");
            }
        }

        return analysis;
    }

    /// <summary>
    /// Prints memory profiling results
    /// </summary>
    private void PrintMemoryResults(MemoryProfilingResults results)
    {
        Console.WriteLine("\n=== MEMORY PROFILING RESULTS ===");
        Console.WriteLine($"Memory Growth:");
        Console.WriteLine($"  Initial: {results.InitialMemoryMB:F2} MB");
        Console.WriteLine($"  Final: {results.FinalMemoryMB:F2} MB");
        Console.WriteLine($"  Growth: {results.MemoryGrowthMB:F2} MB ({results.MemoryGrowthPercent:F2}%)");
        Console.WriteLine($"  Iterations: {results.TotalIterations}");

        Console.WriteLine($"\nGC Collections:");
        Console.WriteLine($"  Gen0: {results.GCCollections.Gen0}");
        Console.WriteLine($"  Gen1: {results.GCCollections.Gen1}");
        Console.WriteLine($"  Gen2: {results.GCCollections.Gen2}");

        Console.WriteLine($"\nStress Test:");
        Console.WriteLine($"  Files loaded: {results.StressTestFiles}");
        Console.WriteLine($"  Peak memory: {results.StressTestPeakMemoryMB:F2} MB");
        Console.WriteLine($"  Memory used: {results.StressTestMemoryUsedMB:F2} MB");
        Console.WriteLine($"  Recovered: {results.StressTestRecoveredMB:F2} MB");

        Console.WriteLine($"\nGit Commit: {results.CommitHash}");

        if (results.HasMemoryLeak)
        {
            Console.WriteLine($"\n⚠️  Potential memory leak detected (>200% growth)");
        }
    }

    /// <summary>
    /// Saves memory profiling results to JSON
    /// </summary>
    private void SaveMemoryResults(MemoryProfilingResults results, string milestoneName)
    {
        var benchmarksDir = "benchmarks";
        if (!Directory.Exists(benchmarksDir))
        {
            Directory.CreateDirectory(benchmarksDir);
        }

        var filePath = Path.Combine(benchmarksDir, $"{milestoneName}-memory.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(results, options);
        File.WriteAllText(filePath, json);

        // Also print to console
        Console.WriteLine($"\n=== JSON OUTPUT ({milestoneName}-memory.json) ===");
        Console.WriteLine(json);
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

    /// <summary>
    /// Memory profiling results for PASO 5.4
    /// </summary>
    public class MemoryProfilingResults
    {
        public required BenchmarkEnvironment Environment { get; set; }
        public double InitialMemoryMB { get; set; }
        public double FinalMemoryMB { get; set; }
        public double MemoryGrowthMB { get; set; }
        public double MemoryGrowthPercent { get; set; }
        public int TotalIterations { get; set; }
        public int StressTestFiles { get; set; }
        public double StressTestPeakMemoryMB { get; set; }
        public double StressTestMemoryUsedMB { get; set; }
        public double StressTestRecoveredMB { get; set; }
        public required GCCounts GCCollections { get; set; }
        public List<(int iteration, long memory)> MemoryGrowthSamples { get; set; } = new();
        public bool HasMemoryLeak { get; set; }
        public string CommitHash { get; set; } = "unknown";
    }

    /// <summary>
    /// GC collection counts
    /// </summary>
    public class GCCounts
    {
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
    }

    #endregion
}
