# Logging in ZK-Notes

ZK-Notes uses [Serilog](https://serilog.net/) for comprehensive structured logging throughout the application.

## Log Location

Logs are stored in the `_logs` subdirectory within your notes directory:
- Default: `Documents/ZKNotes/_logs/`
- Filename format: `zknotes-YYYYMMDD.log` (e.g., `zknotes-20260225.log`)

## Log Retention

- **Rolling interval**: Daily (new file each day)
- **Retention**: 7 days (older logs automatically deleted)
- **File size limit**: 10MB per file
- **Output**: File + Debug console (during development)

## Log Levels

### Debug
Detailed diagnostic information for troubleshooting.
- Note ID generation
- Individual note save/load operations
- Search queries and results
- Index operations

### Information
High-level application flow and important state changes.
- Application startup/shutdown
- Service initialization
- Bulk operations (loading all notes, rebuilding search index)
- Successful completion of major operations

### Warning
Potentially problematic situations that don't prevent operation.
- Failed to load malformed note files
- Search queries that fail to parse
- Attempted operations on non-existent resources
- Missing or corrupted metadata

### Error
Error conditions that prevent specific operations from completing.
- File I/O failures during save/delete
- Database errors during search indexing
- Exceptions during critical operations

### Fatal
Critical errors that prevent application startup or force shutdown.
- Failed to initialize services
- Unhandled exceptions in application domain

## Log Format

```
YYYY-MM-DD HH:mm:ss.fff [LEVEL] Message
Exception details (if any)
```

Example:
```
2026-02-25 14:23:45.123 [INF] ZKNotes application starting
2026-02-25 14:23:45.234 [INF] StorageService initialized with directory: C:\Users\...\Documents\ZKNotes
2026-02-25 14:23:45.345 [INF] Loading all notes from directory: C:\Users\...\Documents\ZKNotes
2026-02-25 14:23:45.456 [DBG] Found 42 markdown files
2026-02-25 14:23:46.123 [WRN] Failed to load note file: C:\...\corrupted.md
System.IO.IOException: Unable to read data
2026-02-25 14:23:46.234 [INF] Loaded 41 notes successfully
```

## Structured Logging

Logs use Serilog's structured logging format with named properties:

```csharp
_logger.Information("Saving note: {NoteId} - {NoteTitle}", note.Id, note.Title);
_logger.Debug("Search found {ResultCount} results for query: {Query}", results.Count, query);
_logger.Error(ex, "Failed to delete note: {NoteId}", id);
```

This allows for:
- Easy searching and filtering of logs
- Extraction of specific properties
- Integration with log analysis tools

## Global Exception Handling

ZK-Notes includes two global exception handlers:

### 1. AppDomain Unhandled Exceptions
Catches exceptions that escape all other handlers (usually fatal).
- Logs the exception as **Fatal**
- Shows error dialog to user
- Application terminates

### 2. Dispatcher Unhandled Exceptions
Catches exceptions in UI thread operations.
- Logs the exception as **Error**
- Shows error dialog to user
- Marks exception as handled (application continues)

## Accessing Logs

### For Users
1. Open your notes directory (default: `Documents/ZKNotes/`)
2. Navigate to `_logs/` subdirectory
3. Open the current day's log file with any text editor

### For Developers
During development, logs also appear in the Debug console/output window in Visual Studio.

## Best Practices

### When to Log

**Do log:**
- Entry/exit of critical operations
- Exceptions with context
- State changes in services
- User-impacting events
- Performance metrics for slow operations

**Don't log:**
- Sensitive user data (passwords, API keys)
- Large content payloads
- Inside tight loops (use Debug level sparingly)
- Successful trivial operations

### Log Message Guidelines

**Good:**
```csharp
_logger.Information("Loaded {NoteCount} notes successfully", notes.Count);
_logger.Error(ex, "Failed to save note: {NoteId} - {NoteTitle}", note.Id, note.Title);
```

**Bad:**
```csharp
_logger.Information($"Loaded {notes.Count} notes"); // String interpolation loses structure
_logger.Error(ex.ToString()); // Exception message without context
```

## Adding Logging to New Services

1. Accept `LoggerService` in constructor:
```csharp
private readonly LoggerService _logger;

public MyService(LoggerService logger)
{
    _logger = logger;
}
```

2. Log service initialization:
```csharp
_logger.Information("MyService initialized");
```

3. Log operations with context:
```csharp
public async Task DoSomethingAsync(string id)
{
    _logger.Debug("Starting operation for: {Id}", id);
    
    try
    {
        // Operation logic
        _logger.Information("Operation completed successfully for: {Id}", id);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Operation failed for: {Id}", id);
        throw;
    }
}
```

4. Register in DI container (App.xaml.cs):
```csharp
services.AddSingleton<MyService>();
```

## Configuration

Logging configuration is set in `LoggerService.cs`:

- **Minimum level**: Debug (all levels logged)
- **Microsoft libraries**: Warning and above only
- **Sinks**: Debug console + File
- **File path**: `{NotesDirectory}/_logs/zknotes-.log`

To change configuration, modify the `LoggerConfiguration` in the `LoggerService` constructor.

## Troubleshooting

### No logs appearing
1. Check that the application has write permissions to the notes directory
2. Verify logs directory exists: `{NotesDirectory}/_logs/`
3. Check for log files with today's date

### Log file locked
Logs are written continuously while the application runs. Close ZK-Notes before opening log files in exclusive-access editors.

### Logs too verbose
Change minimum level in `LoggerService.cs`:
```csharp
.MinimumLevel.Information() // Only Info, Warning, Error, Fatal
```

### Logs not detailed enough
Ensure Debug level is enabled and check the code is logging at appropriate levels.

## Performance Considerations

Logging has minimal performance impact:
- Asynchronous file writes
- Structured logging avoids string formatting overhead
- File size limits prevent unbounded growth
- Automatic cleanup of old logs

Debug-level logging in hot paths may have slight impact—use sparingly in performance-critical code.
