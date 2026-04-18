# ZK-Notes
ZK-Notes is a local-first Zettelkasten workspace delivered as a desktop app through Electron with a Svelte renderer.

## Current GUI Direction
- Electron is the primary GUI target for this repository.
- The desktop UI has been refocused toward a cleaner Notion-like workspace: page-first navigation, restrained chrome, and simplified editing surfaces.
- Graph-centric and alternate GUI surface elements have been removed from active navigation/workflow.

## Features
- Page-first notes workspace
- Markdown editor with preview and split mode
- `[[wiki-links]]` with backlinks
- `#hashtag` extraction and filtering support
- Quick capture (`Ctrl+Shift+N`)
- Command palette (`Ctrl+K`)
- Trash and restore workflow
- Light/dark themes
- Local persistence via IndexedDB

## Run in Development
```sh
cd desktop
npm install
npm run dev
```

## Package Desktop Builds
```sh
cd desktop
npm run dist
```

## Project Structure
```text
desktop/                   Electron shell and packaging
  src/main.cjs             Main process, window lifecycle, static serving
  src/preload.cjs          Secure renderer bridge for window controls/shortcuts

web/                       Svelte renderer consumed by Electron
  src/routes/+layout.svelte    App shell (titlebar + sidebar)
  src/routes/+page.svelte      Notes index/list page
  src/routes/note/[id]/+page.svelte   Note editor page
  src/routes/trash/+page.svelte       Trash workflow
  src/lib/noteStore.svelte.ts         Reactive note store
```
