using System;
using System.Diagnostics;
using ZKNotes.Services;

namespace ZKNotes.Helpers;

/// <summary>
/// Helper for timing operations and logging slow performance.
/// Usage: using (PerformanceTimer.Start(logger, "Operation name")) { ... }
/// </summary>
public sealed class PerformanceTimer : IDisposable
{
    private readonly LoggerService _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly int _warningThresholdMs;

    private PerformanceTimer(LoggerService logger, string operationName, int warningThresholdMs)
    {
        _logger = logger;
        _operationName = operationName;
        _warningThresholdMs = warningThresholdMs;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Starts timing an operation.
    /// </summary>
    /// <param name="logger">Logger service</param>
    /// <param name="operationName">Name of the operation being timed</param>
    /// <param name="warningThresholdMs">Log warning if operation exceeds this duration (default: 1000ms)</param>
    public static PerformanceTimer Start(LoggerService logger, string operationName, int warningThresholdMs = 1000)
    {
        return new PerformanceTimer(logger, operationName, warningThresholdMs);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var elapsedMs = _stopwatch.ElapsedMilliseconds;

        if (elapsedMs >= _warningThresholdMs)
        {
            _logger.Warning("Slow operation detected: {OperationName} took {ElapsedMs}ms", 
                _operationName, elapsedMs);
        }
        else if (elapsedMs >= 100)
        {
            _logger.Debug("Operation {OperationName} took {ElapsedMs}ms", 
                _operationName, elapsedMs);
        }
    }
}
