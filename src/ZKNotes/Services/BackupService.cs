using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ZKNotes.Models;

namespace ZKNotes.Services;

/// <summary>
/// Handles backup and recovery operations for notes and application data.
/// Creates timestamped ZIP archives with notes, metadata, and configuration.
/// </summary>
public sealed class BackupService
{
    private const string BackupSubfolder = "_backups";
    private const string AutoBackupPrefix = "auto_";
    private const string ManualBackupPrefix = "manual_";
    private const string BackupMetadataFile = "backup_metadata.json";
    
    private readonly string _notesDir;
    private readonly string _backupDir;
    private readonly LoggerService _logger;

    public BackupService(string notesDirectory, LoggerService logger)
    {
        _notesDir = notesDirectory;
        _backupDir = Path.Combine(notesDirectory, BackupSubfolder);
        _logger = logger;
        
        Directory.CreateDirectory(_backupDir);
        _logger.Information("BackupService initialized. Backup directory: {BackupDirectory}", _backupDir);
    }

    public string BackupDirectory => _backupDir;

    /// <summary>
    /// Creates an automatic backup before a destructive operation.
    /// </summary>
    public async Task<string?> CreateAutoBackupAsync(string reason)
    {
        _logger.Information("Creating automatic backup. Reason: {Reason}", reason);
        
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"{AutoBackupPrefix}{timestamp}.zip";
            var backupPath = Path.Combine(_backupDir, filename);

            var metadata = new BackupMetadata
            {
                CreatedAt = DateTime.Now,
                BackupType = BackupType.Automatic,
                Reason = reason,
                NoteCount = 0 // Will be updated during backup
            };

            await CreateBackupArchiveAsync(backupPath, metadata).ConfigureAwait(false);
            
            _logger.Information("Automatic backup created: {BackupPath}", backupPath);
            
            // Cleanup old auto backups
            await CleanupOldAutoBackupsAsync().ConfigureAwait(false);
            
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create automatic backup for reason: {Reason}", reason);
            return null;
        }
    }

    /// <summary>
    /// Creates a manual backup initiated by the user.
    /// </summary>
    public async Task<string> CreateManualBackupAsync(string? description = null)
    {
        _logger.Information("Creating manual backup");
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{ManualBackupPrefix}{timestamp}.zip";
        var backupPath = Path.Combine(_backupDir, filename);

        var metadata = new BackupMetadata
        {
            CreatedAt = DateTime.Now,
            BackupType = BackupType.Manual,
            Description = description,
            NoteCount = 0
        };

        await CreateBackupArchiveAsync(backupPath, metadata).ConfigureAwait(false);
        
        _logger.Information("Manual backup created: {BackupPath}", backupPath);
        
        return backupPath;
    }

    /// <summary>
    /// Exports all notes to a ZIP file at a user-specified location.
    /// </summary>
    public async Task<string> ExportNotesAsync(string exportPath, bool includeConfig = false)
    {
        _logger.Information("Exporting notes to: {ExportPath}", exportPath);
        
        var metadata = new BackupMetadata
        {
            CreatedAt = DateTime.Now,
            BackupType = BackupType.Export,
            Description = "User export",
            NoteCount = 0
        };

        await CreateBackupArchiveAsync(exportPath, metadata, includeConfig).ConfigureAwait(false);
        
        _logger.Information("Notes exported successfully to: {ExportPath}", exportPath);
        
        return exportPath;
    }

    /// <summary>
    /// Restores notes from a backup archive.
    /// Creates a backup of current state before restoring.
    /// </summary>
    public async Task<RestoreResult> RestoreFromBackupAsync(string backupPath, bool createBackupFirst = true)
    {
        _logger.Information("Starting restore from backup: {BackupPath}", backupPath);
        
        if (!File.Exists(backupPath))
        {
            _logger.Error("Backup file not found: {BackupPath}", backupPath);
            throw new FileNotFoundException("Backup file not found", backupPath);
        }

        string? preRestoreBackup = null;
        
        try
        {
            // Create backup of current state before restoring
            if (createBackupFirst)
            {
                preRestoreBackup = await CreateAutoBackupAsync("Pre-restore safety backup").ConfigureAwait(false);
            }

            // Read backup metadata
            var metadata = await ReadBackupMetadataAsync(backupPath).ConfigureAwait(false);
            
            // Extract to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), $"zk_restore_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            
            try
            {
                _logger.Debug("Extracting backup to temporary directory: {TempDir}", tempDir);
                ZipFile.ExtractToDirectory(backupPath, tempDir);

                // Count files to restore
                var notesToRestore = Directory.GetFiles(tempDir, "*.md", SearchOption.TopDirectoryOnly);
                _logger.Information("Found {FileCount} notes to restore", notesToRestore.Length);

                // Restore notes
                var restoredCount = 0;
                var skippedCount = 0;

                foreach (var file in notesToRestore)
                {
                    var filename = Path.GetFileName(file);
                    var targetPath = Path.Combine(_notesDir, filename);
                    
                    // Copy file to notes directory
                    File.Copy(file, targetPath, overwrite: true);
                    restoredCount++;
                }

                _logger.Information("Restore completed. Restored: {RestoredCount}, Skipped: {SkippedCount}", 
                    restoredCount, skippedCount);

                return new RestoreResult
                {
                    Success = true,
                    RestoredCount = restoredCount,
                    SkippedCount = skippedCount,
                    BackupMetadata = metadata,
                    PreRestoreBackupPath = preRestoreBackup
                };
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restore from backup: {BackupPath}", backupPath);
            
            return new RestoreResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                PreRestoreBackupPath = preRestoreBackup
            };
        }
    }

    /// <summary>
    /// Imports notes from an external ZIP file without replacing existing notes.
    /// </summary>
    public Task<ImportResult> ImportNotesAsync(string importPath, bool skipDuplicates = true)
    {
        _logger.Information("Importing notes from: {ImportPath}", importPath);
        
        if (!File.Exists(importPath))
        {
            throw new FileNotFoundException("Import file not found", importPath);
        }

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"zk_import_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            
            try
            {
                ZipFile.ExtractToDirectory(importPath, tempDir);

                var notesToImport = Directory.GetFiles(tempDir, "*.md", SearchOption.TopDirectoryOnly);
                _logger.Information("Found {FileCount} notes to import", notesToImport.Length);

                var importedCount = 0;
                var skippedCount = 0;

                foreach (var file in notesToImport)
                {
                    var filename = Path.GetFileName(file);
                    var targetPath = Path.Combine(_notesDir, filename);
                    
                    if (File.Exists(targetPath) && skipDuplicates)
                    {
                        _logger.Debug("Skipping duplicate: {Filename}", filename);
                        skippedCount++;
                        continue;
                    }

                    File.Copy(file, targetPath, overwrite: !skipDuplicates);
                    importedCount++;
                }

                _logger.Information("Import completed. Imported: {ImportedCount}, Skipped: {SkippedCount}", 
                    importedCount, skippedCount);

                return Task.FromResult(new ImportResult
                {
                    Success = true,
                    ImportedCount = importedCount,
                    SkippedCount = skippedCount
                });
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to import notes from: {ImportPath}", importPath);
            
            return Task.FromResult(new ImportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Lists all available backups sorted by date (newest first).
    /// </summary>
    public List<BackupInfo> ListBackups()
    {
        if (!Directory.Exists(_backupDir))
            return new List<BackupInfo>();

        var backups = new List<BackupInfo>();
        var files = Directory.GetFiles(_backupDir, "*.zip", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var filename = Path.GetFileName(file);
                var isAuto = filename.StartsWith(AutoBackupPrefix, StringComparison.OrdinalIgnoreCase);
                
                backups.Add(new BackupInfo
                {
                    FilePath = file,
                    FileName = filename,
                    CreatedAt = fileInfo.CreationTime,
                    SizeBytes = fileInfo.Length,
                    IsAutomatic = isAuto
                });
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to read backup info for file: {FilePath}", file);
            }
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <summary>
    /// Deletes a specific backup file.
    /// </summary>
    public void DeleteBackup(string backupPath)
    {
        _logger.Information("Deleting backup: {BackupPath}", backupPath);
        
        if (!File.Exists(backupPath))
        {
            _logger.Warning("Backup file not found: {BackupPath}", backupPath);
            return;
        }

        try
        {
            File.Delete(backupPath);
            _logger.Information("Backup deleted: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete backup: {BackupPath}", backupPath);
            throw;
        }
    }

    private async Task CreateBackupArchiveAsync(string backupPath, BackupMetadata metadata, bool includeConfig = true)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"zk_backup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Copy all markdown files
            var noteFiles = Directory.GetFiles(_notesDir, "*.md", SearchOption.TopDirectoryOnly);
            metadata.NoteCount = noteFiles.Length;

            _logger.Debug("Copying {NoteCount} notes to temp directory", noteFiles.Length);
            
            foreach (var file in noteFiles)
            {
                var filename = Path.GetFileName(file);
                var destPath = Path.Combine(tempDir, filename);
                File.Copy(file, destPath);
            }

            // Write backup metadata
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, BackupMetadataFile), metadataJson).ConfigureAwait(false);

            // Create ZIP archive
            if (File.Exists(backupPath))
                File.Delete(backupPath);
                
            ZipFile.CreateFromDirectory(tempDir, backupPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            
            _logger.Debug("Backup archive created: {BackupPath}, Size: {SizeBytes} bytes", 
                backupPath, new FileInfo(backupPath).Length);
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private async Task<BackupMetadata?> ReadBackupMetadataAsync(string backupPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(backupPath);
            var metadataEntry = archive.GetEntry(BackupMetadataFile);
            
            if (metadataEntry == null)
            {
                _logger.Warning("Backup metadata not found in archive: {BackupPath}", backupPath);
                return null;
            }

            using var stream = metadataEntry.Open();
            return await JsonSerializer.DeserializeAsync<BackupMetadata>(stream).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to read backup metadata from: {BackupPath}", backupPath);
            return null;
        }
    }

    private async Task CleanupOldAutoBackupsAsync()
    {
        const int MaxAutoBackups = 10;
        
        try
        {
            var autoBackups = Directory.GetFiles(_backupDir, $"{AutoBackupPrefix}*.zip", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (autoBackups.Count > MaxAutoBackups)
            {
                var toDelete = autoBackups.Skip(MaxAutoBackups).ToList();
                _logger.Information("Cleaning up {Count} old automatic backups", toDelete.Count);

                foreach (var backup in toDelete)
                {
                    try
                    {
                        backup.Delete();
                        _logger.Debug("Deleted old backup: {FileName}", backup.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to delete old backup: {FilePath}", backup.FullName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to cleanup old automatic backups");
        }

        await Task.CompletedTask;
    }
}

public enum BackupType
{
    Automatic,
    Manual,
    Export
}

public sealed class BackupMetadata
{
    public DateTime CreatedAt { get; set; }
    public BackupType BackupType { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public int NoteCount { get; set; }
    public string AppVersion { get; set; } = "1.0.0";
}

public sealed class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public bool IsAutomatic { get; set; }
    
    public string SizeFormatted => FormatBytes(SizeBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public sealed class RestoreResult
{
    public bool Success { get; set; }
    public int RestoredCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public BackupMetadata? BackupMetadata { get; set; }
    public string? PreRestoreBackupPath { get; set; }
}

public sealed class ImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
}
