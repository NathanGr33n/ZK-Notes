# ZK-Notes Web

SvelteKit web frontend for the ZK-Notes Zettelkasten knowledge system.

## Tech Stack

- **SvelteKit 2** with Svelte 5 runes
- **Tailwind CSS 4** + **DaisyUI 5**
- **GSAP** for animations
- **marked** for Markdown rendering

## Development

```sh
npm install
npm run dev
```

## Scripts

- `npm run dev` — Start dev server at [localhost:5173](http://localhost:5173)
- `npm run check` — Run TypeScript and Svelte diagnostics
- `npm run build` — Production build
- `npm run preview` — Preview production build locally

## Architecture

- `src/lib/types.ts` — Core types (`Note`, `NoteType`, `NoteMetadata`)
- `src/lib/noteStore.svelte.ts` — Reactive store with CRUD, search, and backlink index
- `src/lib/themeStore.svelte.ts` — Light/dark theme with localStorage persistence
- `src/lib/linkParser.ts` — `[[wiki-link]]` extraction and HTML rendering
- `src/lib/tagParser.ts` — `#hashtag` extraction from Markdown content
- `src/lib/components/` — `NoteCard`, `LinkAutocomplete`, `LinkPreview`
- `src/routes/` — Root layout (sidebar + workspace), home page (bento grid), note editor
