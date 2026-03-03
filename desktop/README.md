# ZK-Notes Desktop (Electron)

This package wraps the SvelteKit UI from `../web` into a cross-platform desktop application using Electron.

## Dev

From `desktop/`:

```sh
npm install
npm run dev
```

This starts the SvelteKit dev server (port 5173) and launches Electron pointed at it.

## Build & Run (local)

```sh
npm run start
```

This builds the SvelteKit app and launches Electron loading the built output.

## Distributables

```sh
npm run dist
```

Outputs installers/bundles into `desktop/dist/`.
