using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MqlLanguageServer.Lsp.Server;

/// <summary>
/// Helper class for monitoring performance of operations
/// </summary>
public class PerformanceMonitor
{
    private readonly MetricsCollector _metrics;
    private readonly ILogger? _logger;

    public PerformanceMonitor(MetricsCollector metrics, ILogger? logger = null)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger;
    }

    /// <summary>
    /// Monitor an operation with automatic timing and metrics recording
    /// </summary>
    public OperationMonitor MonitorOperation(string operationName, string? filePath = null, LogLevel logLevel = LogLevel.Debug)
    {
        return new OperationMonitor(_metrics, operationName, filePath, _logger, logLevel);
    }

    /// <summary>
    /// Record parsing time with metrics
    /// </summary>
    public void RecordParsingTime(string filePath, TimeSpan duration, int symbolsParsed, bool fromCache = false)
    {
        _metrics.RecordParsingTime(filePath, duration, symbolsParsed, fromCache);

        _logger?.LogDebug(
            "Parsed {FilePath}: {Duration}ms ({Symbols} symbols, from cache: {FromCache})",
            filePath,
            duration.TotalMilliseconds,
            symbolsParsed,
            fromCache);
    }
}

/// <summary>
/// Disposable wrapper for monitoring an operation
/// </summary>
public class OperationMonitor : IDisposable
{
    private readonly MetricsCollector _metrics;
    private readonly string _operationName;
    private readonly string? _filePath;
    private readonly ILogger? _logger;
    private readonly LogLevel _logLevel;
    private readonly Stopwatch _stopwatch;
    private bool _disposed = false;

    public OperationMonitor(
        MetricsCollector metrics,
        string operationName,
        string? filePath,
        ILogger? logger,
        LogLevel logLevel)
    {
        _metrics = metrics;
        _operationName = operationName;
        _filePath = filePath;
        _logger = logger;
        _logLevel = logLevel;
        _stopwatch = Stopwatch.StartNew();

        _logger?.Log(_logLevel, "Starting {Operation} on {FilePath}", _operationName, _filePath ?? "global");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            var duration = _stopwatch.Elapsed;

            _metrics.RecordOperation(_operationName, duration, _filePath, false);

            _logger?.Log(
                _logLevel,
                "Completed {Operation} on {FilePath} in {Duration}ms",
                _operationName,
                _filePath ?? "global",
                duration.TotalMilliseconds);

            _disposed = true;
        }
    }

    /// <summary>
    /// Record an error occurred during the operation
    /// </summary>
    public void RecordError(Exception ex)
    {
        _metrics.RecordOperation(_operationName, _stopwatch.Elapsed, _filePath, true);
        _logger?.LogError(ex, "Error in {Operation} on {FilePath}", _operationName, _filePath ?? "global");
    }
}
