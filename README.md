# ZK-Notes

A Zettelkasten-style personal knowledge management app for Windows.

## Features

- **Fast note capture** — Ctrl+Shift+N popup for instant note creation
- **Markdown editor** with live HTML preview (WebView2)
- **`[[wiki-links]]`** and inline **#hashtags** parsed automatically
- **Full-text search** powered by SQLite FTS5
- **Tag-based filtering** across all notes
- **Interactive graph** — force-directed visualization of note relationships (zoom, pan, click)
- **Weekly review** — surfaces notes not reviewed in the past 7 days
- **Local-first** — notes stored as `.md` files with YAML frontmatter; no cloud required

## Tech Stack

- .NET 9 / WPF (Windows Presentation Foundation)
- CommunityToolkit.Mvvm (MVVM source generators)
- Markdig (Markdown parsing)
- YamlDotNet (frontmatter serialization)
- Microsoft.Data.Sqlite (FTS5 search index)
- Microsoft.Web.WebView2 (Markdown preview)

## Build & Run

```
dotnet restore
dotnet build
dotnet run --project src/ZKNotes
```

Requires .NET 9 SDK and WebView2 Runtime (included in Windows 11 / Edge).

## Testing

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal
```

The test suite includes 59 tests covering:
- Link parsing (`[[wiki-links]]`)
- Tag extraction (`#hashtags`)
- Note storage and persistence
- YAML frontmatter serialization

See [tests/ZKNotes.Tests/README.md](tests/ZKNotes.Tests/README.md) for details.

## Project Structure

```
src/ZKNotes/
  Models/        — Note, NoteMetadata, AppConfig
  ViewModels/    — MainViewModel, NoteEditorViewModel, SearchViewModel, GraphViewModel, ReviewViewModel
  Views/         — XAML views + code-behind
  Services/      — StorageService (file I/O), SearchService (SQLite FTS5)
  Helpers/       — LinkParser, TagParser, MarkdownHelper
  Converters/    — WPF value converters
```

Notes are stored as Markdown files in `Documents/ZKNotes/` with YAML frontmatter containing metadata (ID, title, tags, links, timestamps).
