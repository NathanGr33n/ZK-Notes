using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace ZKNotes.Services;

/// <summary>
/// Centralized logging service using Serilog.
/// Logs to both file and debug output.
/// </summary>
public sealed class LoggerService : IDisposable
{
    private readonly ILogger _logger;

    public LoggerService(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);

        var logFilePath = Path.Combine(logsDirectory, "zknotes-.log");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "ZKNotes")
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_485_760) // 10MB
            .CreateLogger();

        _logger.Information("ZKNotes logging initialized");
        _logger.Information("Log directory: {LogDirectory}", logsDirectory);
    }

    public void Debug(string message) => _logger.Debug(message);
    public void Debug(string messageTemplate, params object[] propertyValues) => _logger.Debug(messageTemplate, propertyValues);

    public void Information(string message) => _logger.Information(message);
    public void Information(string messageTemplate, params object[] propertyValues) => _logger.Information(messageTemplate, propertyValues);

    public void Warning(string message) => _logger.Warning(message);
    public void Warning(string messageTemplate, params object[] propertyValues) => _logger.Warning(messageTemplate, propertyValues);
    public void Warning(Exception exception, string message) => _logger.Warning(exception, message);
    public void Warning(Exception exception, string messageTemplate, params object[] propertyValues) => _logger.Warning(exception, messageTemplate, propertyValues);

    public void Error(Exception exception, string message) => _logger.Error(exception, message);
    public void Error(Exception exception, string messageTemplate, params object[] propertyValues) => _logger.Error(exception, messageTemplate, propertyValues);
    public void Error(string message) => _logger.Error(message);
    public void Error(string messageTemplate, params object[] propertyValues) => _logger.Error(messageTemplate, propertyValues);

    public void Fatal(Exception exception, string message) => _logger.Fatal(exception, message);
    public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues) => _logger.Fatal(exception, messageTemplate, propertyValues);

    public void Dispose()
    {
        _logger.Information("Shutting down logging");
        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
