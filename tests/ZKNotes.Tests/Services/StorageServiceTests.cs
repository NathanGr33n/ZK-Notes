using FluentAssertions;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.Tests.Services;

public class StorageServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly StorageService _storage;

    public StorageServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"zktest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _storage = new StorageService(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_CreatesNotesDirectory()
    {
        // Assert
        Directory.Exists(_testDir).Should().BeTrue();
    }

    [Fact]
    public void NotesDirectory_ReturnsCorrectPath()
    {
        // Act
        var result = _storage.NotesDirectory;

        // Assert
        result.Should().Be(_testDir);
    }

    [Fact]
    public async Task GenerateNextIdAsync_FirstCall_ReturnsZK0001()
    {
        // Act
        var id = await _storage.GenerateNextIdAsync();

        // Assert
        id.Should().Be("ZK-0001");
    }

    [Fact]
    public async Task GenerateNextIdAsync_MultipleCalls_ReturnsIncrementingIds()
    {
        // Act
        var id1 = await _storage.GenerateNextIdAsync();
        var id2 = await _storage.GenerateNextIdAsync();
        var id3 = await _storage.GenerateNextIdAsync();

        // Assert
        id1.Should().Be("ZK-0001");
        id2.Should().Be("ZK-0002");
        id3.Should().Be("ZK-0003");
    }

    [Fact]
    public async Task GenerateNextIdAsync_PersistsCounter()
    {
        // Arrange
        await _storage.GenerateNextIdAsync();
        await _storage.GenerateNextIdAsync();

        // Create new instance with same directory
        var newStorage = new StorageService(_testDir);

        // Act
        var id = await newStorage.GenerateNextIdAsync();

        // Assert
        id.Should().Be("ZK-0003");
    }

    [Fact]
    public async Task SaveNoteAsync_WithValidNote_SavesFile()
    {
        // Arrange
        var note = new Note
        {
            Id = "ZK-0001",
            Title = "Test Note",
            Content = "This is test content.",
            Tags = ["test", "example"],
            Created = new DateTime(2024, 1, 1, 12, 0, 0),
            LastEdit = new DateTime(2024, 1, 1, 12, 30, 0)
        };

        // Act
        await _storage.SaveNoteAsync(note);

        // Assert
        var filePath = Path.Combine(_testDir, "ZK-0001.md");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveNoteAsync_WithNullNote_ThrowsException()
    {
        // Act
        var act = async () => await _storage.SaveNoteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveNoteAsync_WithEmptyId_ThrowsException()
    {
        // Arrange
        var note = new Note { Id = string.Empty, Title = "Test" };

        // Act
        var act = async () => await _storage.SaveNoteAsync(note);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveNoteAsync_UpdatesLastEditTimestamp()
    {
        // Arrange
        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var note = new Note
        {
            Id = "ZK-0001",
            Title = "Test",
            Content = "Content",
            LastEdit = originalTime
        };

        // Act
        await Task.Delay(10); // Ensure time difference
        await _storage.SaveNoteAsync(note);

        // Assert
        note.LastEdit.Should().BeAfter(originalTime);
    }

    [Fact]
    public async Task LoadNoteAsync_WithExistingNote_LoadsNote()
    {
        // Arrange
        var originalNote = new Note
        {
            Id = "ZK-0001",
            Title = "Test Note",
            Content = "Test content",
            Tags = ["test"],
            Links = ["ZK-0002"],
            Created = new DateTime(2024, 1, 1, 12, 0, 0),
            LastEdit = new DateTime(2024, 1, 1, 12, 30, 0)
        };
        await _storage.SaveNoteAsync(originalNote);

        // Act
        var loadedNote = await _storage.LoadNoteAsync("ZK-0001");

        // Assert
        loadedNote.Should().NotBeNull();
        loadedNote!.Id.Should().Be("ZK-0001");
        loadedNote.Title.Should().Be("Test Note");
        loadedNote.Content.Should().Be("Test content");
        loadedNote.Tags.Should().BeEquivalentTo(new[] { "test" });
        loadedNote.Links.Should().BeEquivalentTo(new[] { "ZK-0002" });
    }

    [Fact]
    public async Task LoadNoteAsync_WithNonExistentNote_ReturnsNull()
    {
        // Act
        var result = await _storage.LoadNoteAsync("ZK-9999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAllNotesAsync_WithMultipleNotes_LoadsAllNotes()
    {
        // Arrange
        var note1 = new Note { Id = "ZK-0001", Title = "Note 1", Content = "Content 1" };
        var note2 = new Note { Id = "ZK-0002", Title = "Note 2", Content = "Content 2" };
        var note3 = new Note { Id = "ZK-0003", Title = "Note 3", Content = "Content 3" };

        await _storage.SaveNoteAsync(note1);
        await _storage.SaveNoteAsync(note2);
        await _storage.SaveNoteAsync(note3);

        // Act
        var notes = await _storage.LoadAllNotesAsync();

        // Assert
        notes.Should().HaveCount(3);
        notes.Should().Contain(n => n.Id == "ZK-0001");
        notes.Should().Contain(n => n.Id == "ZK-0002");
        notes.Should().Contain(n => n.Id == "ZK-0003");
    }

    [Fact]
    public async Task LoadAllNotesAsync_WithEmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var notes = await _storage.LoadAllNotesAsync();

        // Assert
        notes.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAllNotesAsync_WithMalformedFile_SkipsMalformedFile()
    {
        // Arrange
        var validNote = new Note { Id = "ZK-0001", Title = "Valid", Content = "Valid" };
        await _storage.SaveNoteAsync(validNote);

        // Create malformed file
        var malformedPath = Path.Combine(_testDir, "ZK-0002.md");
        await File.WriteAllTextAsync(malformedPath, "");

        // Act
        var notes = await _storage.LoadAllNotesAsync();

        // Assert
        notes.Should().ContainSingle();
        notes[0].Id.Should().Be("ZK-0001");
    }

    [Fact]
    public async Task DeleteNoteAsync_WithExistingNote_DeletesFile()
    {
        // Arrange
        var note = new Note { Id = "ZK-0001", Title = "Test", Content = "Test" };
        await _storage.SaveNoteAsync(note);
        var filePath = Path.Combine(_testDir, "ZK-0001.md");

        // Act
        await _storage.DeleteNoteAsync("ZK-0001");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteNoteAsync_WithNonExistentNote_DoesNotThrow()
    {
        // Act
        var act = async () => await _storage.DeleteNoteAsync("ZK-9999");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NoteExists_WithExistingNote_ReturnsTrue()
    {
        // Arrange
        var note = new Note { Id = "ZK-0001", Title = "Test", Content = "Test" };
        await _storage.SaveNoteAsync(note);

        // Act
        var exists = _storage.NoteExists("ZK-0001");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void NoteExists_WithNonExistentNote_ReturnsFalse()
    {
        // Act
        var exists = _storage.NoteExists("ZK-9999");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesAllMetadata()
    {
        // Arrange
        var originalNote = new Note
        {
            Id = "ZK-0001",
            Title = "Complex Note",
            Content = "# Heading\n\nContent with [[ZK-0002]] and #tags",
            Type = NoteType.Fleeting,
            Template = "journal",
            Tags = ["research", "idea", "philosophy"],
            Links = ["ZK-0002", "ZK-0003"],
            Attachments = ["image.png"],
            Created = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Local),
            LastEdit = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Local),
            LastReviewed = new DateTime(2024, 1, 3, 9, 0, 0, DateTimeKind.Local)
        };

        // Act
        await _storage.SaveNoteAsync(originalNote);
        var loadedNote = await _storage.LoadNoteAsync("ZK-0001");

        // Assert
        loadedNote.Should().NotBeNull();
        loadedNote!.Id.Should().Be(originalNote.Id);
        loadedNote.Title.Should().Be(originalNote.Title);
        loadedNote.Content.Should().Be(originalNote.Content);
        loadedNote.Type.Should().Be(originalNote.Type);
        loadedNote.Template.Should().Be(originalNote.Template);
        loadedNote.Tags.Should().BeEquivalentTo(originalNote.Tags);
        loadedNote.Links.Should().BeEquivalentTo(originalNote.Links);
        loadedNote.Attachments.Should().BeEquivalentTo(originalNote.Attachments);
        loadedNote.Created.Should().BeCloseTo(originalNote.Created, TimeSpan.FromSeconds(1));
        loadedNote.LastReviewed.Should().BeCloseTo(originalNote.LastReviewed!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SaveNoteAsync_WithSpecialCharactersInId_SanitizesFilename()
    {
        // Arrange
        var note = new Note
        {
            Id = "ZK-0001",
            Title = "Test",
            Content = "Test"
        };

        // Act
        await _storage.SaveNoteAsync(note);

        // Assert
        var files = Directory.GetFiles(_testDir, "*.md");
        files.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadNoteAsync_WithNoteWithoutFrontmatter_LoadsAsPlainContent()
    {
        // Arrange
        var noteId = "ZK-0001";
        var plainContent = "This is a plain markdown note without frontmatter.";
        var filePath = Path.Combine(_testDir, $"{noteId}.md");
        await File.WriteAllTextAsync(filePath, plainContent);

        // Act
        var note = await _storage.LoadNoteAsync(noteId);

        // Assert
        note.Should().NotBeNull();
        note!.Content.Should().Be(plainContent);
        note.Id.Should().Be(noteId);
    }

    [Fact]
    public async Task SaveNoteAsync_ConcurrentCalls_HandlesThreadSafely()
    {
        // Arrange
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            var note = new Note
            {
                Id = $"ZK-{i:D4}",
                Title = $"Note {i}",
                Content = $"Content {i}"
            };
            await _storage.SaveNoteAsync(note);
        });

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var notes = await _storage.LoadAllNotesAsync();
        notes.Should().HaveCount(10);
    }
}
