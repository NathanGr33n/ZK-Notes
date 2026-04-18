// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	interface Window {
		electronAPI?: {
			minimize: () => void;
			maximize: () => void;
			close: () => void;
			isMaximized: () => Promise<boolean>;
			onMaximizedChanged: (callback: (isMaximized: boolean) => void) => () => void;
			onQuickCapture: (callback: (...args: unknown[]) => void) => () => void;
			isElectron: boolean;
		};
	}
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
}

export {};
