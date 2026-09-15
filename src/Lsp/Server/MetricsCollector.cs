using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Thread-safe metrics collector for tracking performance and operations
/// </summary>
public class MetricsCollector
{
    private static readonly Lazy<MetricsCollector> _instance = new(() => new MetricsCollector());
    public static MetricsCollector Instance => _instance.Value;

    // Cache metrics
    private long _cacheHits;
    private long _cacheMisses;

    // Parse-reuse metrics (issue #36): LRU cache hits across didClose cycles
    private long _parseReuses;

    // Parsing metrics
    private readonly ConcurrentDictionary<string, ParsingMetric> _parsingMetrics = new();
    
    // LSP operation metrics
    private readonly ConcurrentDictionary<string, LspOperationMetric> _operationMetrics = new();

    // Slow operation threshold (ms)
    private const int SlowOperationThresholdMs = 100;

    private MetricsCollector() { }

    /// <summary>
    /// Record a cache hit
    /// </summary>
    public void RecordCacheHit()
    {
        Interlocked.Increment(ref _cacheHits);
    }

    /// <summary>
    /// Record a cache miss
    /// </summary>
    public void RecordCacheMiss()
    {
        Interlocked.Increment(ref _cacheMisses);
    }

    /// <summary>
    /// Record a parse reuse: a didOpen served from the closed-document LRU
    /// cache instead of a fresh parse (issue #36).
    /// </summary>
    public void RecordParseReuse()
    {
        Interlocked.Increment(ref _parseReuses);
    }

    /// <summary>
    /// Get cache hit rate as a percentage
    /// </summary>
    public double GetCacheHitRate()
    {
        var total = _cacheHits + _cacheMisses;
        return total > 0 ? (_cacheHits * 100.0) / total : 0.0;
    }

    /// <summary>
    /// Record parsing time for a file
    /// </summary>
    public void RecordParsingTime(string filePath, TimeSpan duration, int symbolsParsed, bool fromCache = false)
    {
        var metric = new ParsingMetric
        {
            FilePath = filePath,
            Duration = duration,
            SymbolsParsed = symbolsParsed,
            Timestamp = DateTime.UtcNow,
            FromCache = fromCache
        };

        _parsingMetrics.AddOrUpdate(
            filePath,
            metric,
            (_, __) => metric);
    }

    /// <summary>
    /// Record an LSP operation with timing
    /// </summary>
    public void RecordOperation(string operationName, TimeSpan duration, string? filePath = null, bool isError = false)
    {
        var metric = new LspOperationMetric
        {
            OperationName = operationName,
            Duration = duration,
            FilePath = filePath,
            Timestamp = DateTime.UtcNow,
            IsError = isError
        };

        _operationMetrics.AddOrUpdate(
            $"{operationName}_{filePath ?? "global"}",
            metric,
            (_, __) => metric);

        // Log slow operations
        if (duration.TotalMilliseconds > SlowOperationThresholdMs)
        {
            // Note: We'll inject ILogger later, for now just track the metric
        }
    }

    /// <summary>
    /// Get all current metrics as a snapshot
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            ParseReuses = Interlocked.Read(ref _parseReuses),
            CacheHitRate = GetCacheHitRate(),
            ParsingMetrics = _parsingMetrics.Values.ToList(),
            OperationMetrics = _operationMetrics.Values.ToList(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Reset all metrics
    /// </summary>
    public void Reset()
    {
        _cacheHits = 0;
        _cacheMisses = 0;
        _parseReuses = 0;
        _parsingMetrics.Clear();
        _operationMetrics.Clear();
    }

    /// <summary>
    /// Get a summary report of current metrics
    /// </summary>
    public string GetSummaryReport()
    {
        var snapshot = GetSnapshot();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== MQL4 LSP Metrics Summary ===");
        sb.AppendLine($"Timestamp: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();

        // Cache metrics
        sb.AppendLine("Cache Metrics:");
        sb.AppendLine($"  Cache Hits: {snapshot.CacheHits}");
        sb.AppendLine($"  Cache Misses: {snapshot.CacheMisses}");
        sb.AppendLine($"  Hit Rate: {snapshot.CacheHitRate:F2}%");
        sb.AppendLine($"  Parse Reuses: {snapshot.ParseReuses}");
        sb.AppendLine();

        // Parsing metrics
        if (snapshot.ParsingMetrics.Any())
        {
            sb.AppendLine("Parsing Metrics:");
            var avgParsingTime = snapshot.ParsingMetrics.Average(m => m.Duration.TotalMilliseconds);
            sb.AppendLine($"  Total Parses: {snapshot.ParsingMetrics.Count}");
            sb.AppendLine($"  Average Time: {avgParsingTime:F2}ms");
            sb.AppendLine($"  From Cache: {snapshot.ParsingMetrics.Count(m => m.FromCache)}");
            sb.AppendLine($"  Total Symbols Parsed: {snapshot.ParsingMetrics.Sum(m => m.SymbolsParsed)}");
            sb.AppendLine();
        }

        // LSP operation metrics
        if (snapshot.OperationMetrics.Any())
        {
            sb.AppendLine("LSP Operation Metrics:");
            var groupedOps = snapshot.OperationMetrics.GroupBy(m => m.OperationName);
            foreach (var group in groupedOps)
            {
                var avgTime = group.Average(m => m.Duration.TotalMilliseconds);
                var maxTime = group.Max(m => m.Duration.TotalMilliseconds);
                var errorCount = group.Count(m => m.IsError);
                sb.AppendLine($"  {group.Key}:");
                sb.AppendLine($"    Avg: {avgTime:F2}ms, Max: {maxTime:F2}ms, Errors: {errorCount}");
            }
            sb.AppendLine();
        }

        // Slow operations
        var slowOps = snapshot.OperationMetrics.Where(m => m.Duration.TotalMilliseconds > SlowOperationThresholdMs).ToList();
        if (slowOps.Any())
        {
            sb.AppendLine($"Slow Operations (>{SlowOperationThresholdMs}ms):");
            foreach (var op in slowOps.OrderByDescending(m => m.Duration.TotalMilliseconds).Take(5))
            {
                sb.AppendLine($"  {op.OperationName} ({op.FilePath ?? "global"}): {op.Duration.TotalMilliseconds:F2}ms");
            }
            sb.AppendLine();
        }

        sb.AppendLine("==================================");

        return sb.ToString();
    }
}

/// <summary>
/// Represents a parsing operation metric
/// </summary>
public class ParsingMetric
{
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int SymbolsParsed { get; set; }
    public DateTime Timestamp { get; set; }
    public bool FromCache { get; set; }
}

/// <summary>
/// Represents an LSP operation metric
/// </summary>
public class LspOperationMetric
{
    public string OperationName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? FilePath { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsError { get; set; }
}

/// <summary>
/// Snapshot of all current metrics
/// </summary>
public class MetricsSnapshot
{
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public long ParseReuses { get; set; }
    public double CacheHitRate { get; set; }
    public IReadOnlyList<ParsingMetric> ParsingMetrics { get; set; } = new List<ParsingMetric>();
    public IReadOnlyList<LspOperationMetric> OperationMetrics { get; set; } = new List<LspOperationMetric>();
    public DateTime Timestamp { get; set; }
}
