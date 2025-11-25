using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mql4LanguageServer.Lsp.Server;

/// <summary>
/// Thread-safe provider for generating and managing correlation IDs
/// </summary>
public class CorrelationIdProvider
{
    private static readonly Lazy<CorrelationIdProvider> _instance = new(() => new CorrelationIdProvider());
    public static CorrelationIdProvider Instance => _instance.Value;

    private readonly AsyncLocal<string?> _currentCorrelationId = new();
    private long _counter = 0;

    /// <summary>
    /// Get the current correlation ID for this async context
    /// </summary>
    public string? GetCorrelationId() => _currentCorrelationId.Value;

    /// <summary>
    /// Set a correlation ID for the current async context
    /// </summary>
    public void SetCorrelationId(string correlationId) => _currentCorrelationId.Value = correlationId;

    /// <summary>
    /// Generate a new unique correlation ID
    /// </summary>
    public string GenerateCorrelationId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var counter = Interlocked.Increment(ref _counter);
        return $"REQ-{timestamp}-{counter}";
    }

    /// <summary>
    /// Execute an action with a generated correlation ID
    /// </summary>
    public async Task<T> ExecuteWithCorrelationIdAsync<T>(Func<Task<T>> action)
    {
        var correlationId = GenerateCorrelationId();
        SetCorrelationId(correlationId);

        try
        {
            return await action();
        }
        finally
        {
            _currentCorrelationId.Value = null;
        }
    }

    /// <summary>
    /// Execute an action with a specific correlation ID
    /// </summary>
    public async Task<T> ExecuteWithCorrelationIdAsync<T>(string correlationId, Func<Task<T>> action)
    {
        SetCorrelationId(correlationId);

        try
        {
            return await action();
        }
        finally
        {
            _currentCorrelationId.Value = null;
        }
    }

    /// <summary>
    /// Execute an action with a generated correlation ID
    /// </summary>
    public void ExecuteWithCorrelationId(Action action)
    {
        var correlationId = GenerateCorrelationId();
        SetCorrelationId(correlationId);

        try
        {
            action();
        }
        finally
        {
            _currentCorrelationId.Value = null;
        }
    }
}
