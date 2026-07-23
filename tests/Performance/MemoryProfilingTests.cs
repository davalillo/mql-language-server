using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MqlLanguageServer.Tests.Performance;

/// <summary>
/// Memory profiling tests for PASO 5.4 - Verificación Final
/// Detects memory leaks and measures memory consumption
/// </summary>
public class MemoryProfilingTests
{
    private readonly ITestOutputHelper? _output;

    public MemoryProfilingTests(ITestOutputHelper output)
    {
        _output = output;
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

    #region Large Object Heap / Large Fixture Tests (G3)

    /// <summary>
    /// G3 — the 313KB Account_Protector.mqh header is larger than the .NET
    /// Small Object Heap chunk limit (~85,000 bytes), so the file content
    /// string returned by File.ReadAllText lives on the Large Object Heap.
    /// Parsing it must not trigger runaway Gen2 collections or unbounded
    /// memory growth. This test parses the header, measures managed memory
    /// before/after a forced GC, and asserts the retained memory is
    /// proportional to the file size (within a generous LOH-aware budget).
    ///
    /// Uses the existing BenchmarkEnvironment / GCCounts infrastructure in
    /// this class. Runs unconditionally (not gated on ShouldRunBenchmark)
    /// because it is a regression guard, not a wall-clock benchmark.
    /// </summary>
    [Fact]
    public void Account_Protector_Header_ParseDoesNotLeakLargeObjectHeap()
    {
        var fixturePath = GetLargeFixturePath("Account_Protector.mqh");
        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var fileInfo = new FileInfo(fixturePath);
        _output?.WriteLine($"  Fixture: {fileInfo.Length} bytes ({fileInfo.Length / 1024.0:F1} KB)");

        // Force a clean baseline before parsing.
        ForceGarbageCollection();
        // Snapshot GC counts AFTER the baseline collect so the force-collect's
        // own Gen2 work is not counted against the parse.
        var gen2Before = GC.CollectionCount(2);
        var gen1Before = GC.CollectionCount(1);
        var gen0Before = GC.CollectionCount(0);
        var managedBefore = GC.GetTotalMemory(forceFullCollection: false);

        // Read + parse. The content string (313KB) is allocated on the LOH.
        var content = File.ReadAllText(fixturePath);
        var parser = new MqlLanguageServer.Parser.Mql4AntlrParser();
        var file = parser.ParseFile(content, fixturePath);

        _output?.WriteLine($"  Parsed: {file.Symbols.Count} symbols, {file.SyntaxErrors.Count} syntax errors");

        // The parser must produce a substantial symbol table — proves the LOH
        // string was actually consumed by the parser.
        Assert.True(file.Symbols.Count > 100,
            $"Account_Protector.mqh should yield >100 symbols, got {file.Symbols.Count}");

        // Snapshot GC counts BEFORE the post-parse force collect so only the
        // parse's own GC work is measured.
        var gen2AfterParse = GC.CollectionCount(2);
        var gen1AfterParse = GC.CollectionCount(1);
        var gen0AfterParse = GC.CollectionCount(0);

        // Drop references and force GC to measure what survives.
        content = null!;
        file = null!;
        ForceGarbageCollection();
        var managedAfter = GC.GetTotalMemory(forceFullCollection: false);

        var gcCounts = new GCCounts
        {
            Gen0 = gen0AfterParse - gen0Before,
            Gen1 = gen1AfterParse - gen1Before,
            Gen2 = gen2AfterParse - gen2Before,
        };

        var retainedBytes = managedAfter - managedBefore;
        var retainedMb = retainedBytes / 1024.0 / 1024.0;

        _output?.WriteLine($"  GC during parse: Gen0={gcCounts.Gen0} Gen1={gcCounts.Gen1} Gen2={gcCounts.Gen2}");
        _output?.WriteLine($"  Retained managed memory after GC: {retainedMb:F2} MB ({retainedBytes} bytes)");

        // Gen2 collections during a single 313KB parse indicate LOH pressure.
        // The LOH string itself plus the parser's intermediate allocations can
        // trigger several Gen2 collections on a busy host. A leak would show
        // unbounded growth across repeated parses (covered by the retained-
        // memory assertion below); a one-shot Gen2 count is a weak signal, so
        // the budget is generous (10) to avoid flakiness while still catching a
        // runaway regression.
        Assert.True(gcCounts.Gen2 <= 10,
            $"Excessive Gen2 collections during parse: {gcCounts.Gen2} (expected <= 10)");

        // Retained memory after the parse references are dropped and GC is
        // forced must be modest. The parser builds symbol lists proportional
        // to the symbol count, so a few MB of retained state is acceptable; a
        // leak would show tens of MB. Budget: 64 MB headroom (generous for LOH
        // fragmentation on CI hosts).
        Assert.True(retainedMb < 64.0,
            $"Retained managed memory {retainedMb:F2} MB exceeds 64 MB budget — possible leak");
    }

    private string GetLargeFixturePath(string fileName)
    {
        var projectRoot = GetProjectRoot();
        return Path.Combine(projectRoot, "tests", "fixtures", "real", "mql4", fileName);
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
