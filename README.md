# ZK-Notes

A Zettelkasten-style personal knowledge management system built around atomic, interlinked notes. Available as a **Windows desktop app** (WPF) and a **web app** (SvelteKit).

## Features

- **Zettelkasten workflow** — Fleeting → Literature → Permanent note lifecycle
- **Markdown editor** with live preview
- **`[[wiki-links]]`** with autocomplete and bidirectional backlinks
- **#hashtag** extraction and tag-based filtering
- **Full-text search** across all notes
- **Bento grid workspace** for spatial note exploration (web)
- **Interactive graph** — force-directed visualization of note relationships (desktop)
- **Weekly review** — surfaces notes not reviewed in the past 7 days (desktop)
- **Light & dark themes** — paper-like warm light and deep charcoal dark
- **Local-first** — notes stored as `.md` files with YAML frontmatter; no cloud required

## Web App

The web frontend lives in `web/` and is built with:

- **SvelteKit 2** with Svelte 5 runes (`$state`, `$derived`)
- **Tailwind CSS 4** + **DaisyUI 5** (semantic, JS-free components)
- **GSAP** (card entrance animations, smooth transitions)
- **marked** (Markdown rendering)

### Quick Start

```sh
cd web
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173) in your browser.

### Build & Check

```sh
cd web
npm run check    # TypeScript + Svelte diagnostics
npm run build    # Production build
npm run preview  # Preview production build
```

## Desktop App (WPF)

The original desktop client lives in `src/ZKNotes/` and is built with:

- .NET 9 / WPF (Windows Presentation Foundation)
- CommunityToolkit.Mvvm (MVVM source generators)
- Markdig (Markdown parsing)
- YamlDotNet (frontmatter serialization)
- Microsoft.Data.Sqlite (FTS5 search index)
- Microsoft.Web.WebView2 (Markdown preview)

### Build & Run

```sh
dotnet restore
dotnet build
dotnet run --project src/ZKNotes
```

Requires .NET 9 SDK and WebView2 Runtime (included in Windows 11 / Edge).

### Testing

```sh
dotnet test
```

The test suite includes 59 tests covering link parsing, tag extraction, note storage, and YAML frontmatter serialization. See [tests/ZKNotes.Tests/README.md](tests/ZKNotes.Tests/README.md) for details.

## Project Structure

```
web/                        — SvelteKit web application
  src/lib/types.ts           — Note, NoteType, NoteMetadata
  src/lib/noteStore.svelte.ts — Reactive note store (CRUD, search, backlinks)
  src/lib/themeStore.svelte.ts — Light/dark theme toggle
  src/lib/linkParser.ts      — [[wiki-link]] extraction and rendering
  src/lib/tagParser.ts       — #hashtag extraction
  src/lib/components/        — NoteCard, LinkAutocomplete, LinkPreview
  src/routes/                — Layout, workspace page, note editor

src/ZKNotes/                — WPF desktop application
  Models/                    — Note, NoteMetadata, AppConfig
  ViewModels/                — MainViewModel, NoteEditorViewModel, SearchViewModel, GraphViewModel
  Views/                     — XAML views + code-behind
  Services/                  — StorageService (file I/O), SearchService (SQLite FTS5)
  Helpers/                   — LinkParser, TagParser, MarkdownHelper

design/                     — Design documentation
  DESIGN.md                  — UI/UX design specification
  METHODOLOGY.md             — Zettelkasten methodology guide
  TS.md                      — Technical stack specification
```

Notes are stored as Markdown files with YAML frontmatter containing metadata (ID, title, type, tags, links, timestamps). IDs follow the format `ZK-0001`, `ZK-0002`, etc.
