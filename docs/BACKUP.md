# Backup and Recovery in ZK-Notes

ZK-Notes includes a comprehensive backup and recovery system to protect your notes from accidental deletion, corruption, or system failures.

## Overview

The backup system provides:
- **Automatic backups** before destructive operations
- **Manual backups** on-demand
- **Export/Import** for portability
- **Restore** from any backup
- **Automatic cleanup** of old backups

## Backup Location

All backups are stored in the `_backups` subdirectory within your notes directory:
- Default: `Documents/ZKNotes/_backups/`
- Format: ZIP archives containing all `.md` files and metadata

## Backup Types

### 1. Automatic Backups

Created automatically before potentially destructive operations:
- **Note deletion**: Before deleting any note
- **Bulk operations**: Before tag merges, mass edits, etc.
- **Restore operations**: Before restoring from another backup (safety net)

**Filename format**: `auto_YYYYMMDD_HHmmss.zip`  
**Example**: `auto_20260225_143045.zip`

**Retention**: Maximum 10 automatic backups. Older backups are automatically deleted when this limit is exceeded.

### 2. Manual Backups

Created on-demand by the user through the UI or API.

**Filename format**: `manual_YYYYMMDD_HHmmss.zip`  
**Example**: `manual_20260225_150000.zip`

**Retention**: No automatic deletion. User must manage manually.

### 3. Exports

User-initiated exports to a custom location (e.g., external drive, cloud storage).

**Filename**: User-specified  
**Location**: User-specified  
**Retention**: Not managed by ZK-Notes

## Backup Contents

Each backup contains:
- All `.md` note files
- `backup_metadata.json` with backup information:
  - Creation timestamp
  - Backup type (Automatic/Manual/Export)
  - Reason or description
  - Note count
  - App version

**Not included in backups**:
- Search index (`.zk-search.db`)
- Logs (`_logs/`)
- Attachments (not yet implemented)
- Configuration files

## Creating Backups

### Automatic Backups

Automatic backups happen transparently:
```csharp
// Before note deletion (handled automatically)
await _backup.CreateAutoBackupAsync($"Before deleting note: {note.Title}");
```

No user action required.

### Manual Backups

**Via Code**:
```csharp
// Create manual backup with optional description
var backupPath = await backupService.CreateManualBackupAsync("Weekly backup");
```

**Planned UI**: Backup button in settings/menu (not yet implemented).

### Export Notes

**Via Code**:
```csharp
// Export to specific location
var exportPath = @"C:\MyBackups\zknotes_export.zip";
await backupService.ExportNotesAsync(exportPath);
```

**Planned UI**: Export menu option (not yet implemented).

## Restoring from Backup

### Safety First

When restoring, ZK-Notes automatically creates a safety backup of your current state first (unless disabled).

### Restore Process

```csharp
var result = await backupService.RestoreFromBackupAsync(
    backupPath: @"C:\...\Documents\ZKNotes\_backups\auto_20260225_143045.zip",
    createBackupFirst: true  // Create safety backup first
);

if (result.Success)
{
    Console.WriteLine($"Restored {result.RestoredCount} notes");
    Console.WriteLine($"Safety backup at: {result.PreRestoreBackupPath}");
}
```

**Behavior**:
1. Creates safety backup of current state
2. Extracts backup archive to temporary directory
3. Copies all `.md` files to notes directory (overwrites existing)
4. Returns result with counts and safety backup location

**Important**: Restoring overwrites all existing notes with the same filename. This is why a safety backup is created first.

## Importing Notes

Import allows adding notes from an external ZIP file without replacing existing notes.

```csharp
var result = await backupService.ImportNotesAsync(
    importPath: @"C:\Downloads\imported_notes.zip",
    skipDuplicates: true  // Skip notes that already exist
);

Console.WriteLine($"Imported: {result.ImportedCount}, Skipped: {result.SkippedCount}");
```

**Use cases**:
- Merging notes from another ZK-Notes instance
- Importing archived notes
- Combining note collections

## Managing Backups

### List Backups

```csharp
var backups = backupService.ListBackups();

foreach (var backup in backups)
{
    Console.WriteLine($"{backup.FileName}");
    Console.WriteLine($"  Created: {backup.CreatedAt}");
    Console.WriteLine($"  Size: {backup.SizeFormatted}");
    Console.WriteLine($"  Type: {(backup.IsAutomatic ? "Automatic" : "Manual")}");
}
```

### Delete Backup

```csharp
backupService.DeleteBackup(backupPath);
```

### Automatic Cleanup

Automatic backups are limited to 10 most recent. Older automatic backups are deleted when new ones are created.

Manual backups are never automatically deleted.

## Backup Strategy Recommendations

### For Regular Users

1. **Trust automatic backups** - They happen before risky operations
2. **Create manual backups** before major changes (reorganization, bulk edits)
3. **Export periodically** to external storage (monthly/quarterly)
4. **Clean up old manual backups** occasionally to save disk space

### For Power Users

1. **External backup tool** - Use your OS backup (Time Machine, File History) for the entire notes directory
2. **Cloud sync** - Sync notes directory to Dropbox/OneDrive/Google Drive
3. **Version control** - Consider using Git for the notes directory
4. **Test restores** - Periodically test restoring to ensure backups work

## Recovery Scenarios

### Accidentally Deleted a Note

1. Note was just deleted → Restore from automatic backup created before deletion
2. Look for most recent backup: `auto_YYYYMMDD_HHmmss.zip`
3. Extract ZIP and copy the specific note back, or restore entire backup

### Corrupted Notes Directory

1. Restore from most recent backup (automatic or manual)
2. Application will create safety backup first
3. All notes will be restored to backup state

### Want to Revert to Earlier State

1. Find backup from desired time
2. Restore from that backup
3. Safety backup preserves current state if you change your mind

### Merging Notes from Another Machine

1. Export notes from machine A
2. Import to machine B with `skipDuplicates: true`
3. Only new notes are added; existing notes unchanged

## Backup File Format

Backups are standard ZIP archives. You can extract them with any ZIP utility.

**Contents**:
```
backup_20260225_143045.zip
├── ZK-0001.md
├── ZK-0002.md
├── ZK-0003.md
├── ...
└── backup_metadata.json
```

**Metadata Example**:
```json
{
  "CreatedAt": "2026-02-25T14:30:45.123",
  "BackupType": "Automatic",
  "Reason": "Before deleting note: My Old Note",
  "NoteCount": 42,
  "AppVersion": "1.0.0"
}
```

## Technical Details

### BackupService API

```csharp
// Create backups
Task<string?> CreateAutoBackupAsync(string reason)
Task<string> CreateManualBackupAsync(string? description = null)
Task<string> ExportNotesAsync(string exportPath, bool includeConfig = false)

// Restore/Import
Task<RestoreResult> RestoreFromBackupAsync(string backupPath, bool createBackupFirst = true)
Task<ImportResult> ImportNotesAsync(string importPath, bool skipDuplicates = true)

// Manage
List<BackupInfo> ListBackups()
void DeleteBackup(string backupPath)
```

### Backup Metadata

```csharp
public sealed class BackupMetadata
{
    public DateTime CreatedAt { get; set; }
    public BackupType BackupType { get; set; }  // Automatic, Manual, Export
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public int NoteCount { get; set; }
    public string AppVersion { get; set; }
}
```

### Results

```csharp
public sealed class RestoreResult
{
    public bool Success { get; set; }
    public int RestoredCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public BackupMetadata? BackupMetadata { get; set; }
    public string? PreRestoreBackupPath { get; set; }  // Safety backup location
}

public sealed class ImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## Performance

- **Backup creation**: Fast (1-2 seconds for hundreds of notes)
- **Compression**: Optimal compression level
- **Temporary files**: Automatically cleaned up
- **Thread-safe**: All operations are thread-safe
- **Disk space**: Typical backup is 10-100KB depending on note count and size

## Troubleshooting

### Backup creation fails

**Check**:
- Write permissions to notes directory
- Sufficient disk space
- No other process has files locked

**Logs**: Check `_logs/zknotes-YYYYMMDD.log` for errors

### Restore fails

**Check**:
- Backup file exists and is not corrupted
- Backup file is a valid ZIP archive
- Notes directory is writable

### Backup directory grows too large

**Solution**:
- Delete old manual backups you no longer need
- Automatic backups are limited to 10 and self-clean
- Export important backups to external storage, then delete

### Can't find a backup

**Check**:
- `Documents/ZKNotes/_backups/` directory
- Backups sorted by date (newest first)
- File naming: `auto_*` or `manual_*`

## Future Enhancements

Planned features:
- **UI for backup management** - View, restore, delete backups from UI
- **Scheduled backups** - Daily/weekly automatic backups
- **Cloud backup** - Optional sync to cloud storage
- **Differential backups** - Only backup changed notes
- **Encrypted backups** - Password-protected archives
- **Attachment support** - Include `_attachments/` in backups
