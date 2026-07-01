import { describe, it, expect, beforeAll } from 'vitest';

// ─── DOM + localStorage stubs for Node ──────────────────────────
// The theme store applies attributes to document.documentElement and
// persists to localStorage, reading both at module-eval time. The stubs
// must therefore exist before the store is (dynamically) imported below.

const attrs = new Map<string, string>();
Object.defineProperty(globalThis, 'document', {
	value: {
		documentElement: {
			setAttribute: (key: string, value: string) => { attrs.set(key, value); },
			getAttribute: (key: string) => attrs.get(key) ?? null,
		},
	},
	writable: true,
	configurable: true,
});

const ls = new Map<string, string>();
Object.defineProperty(globalThis, 'localStorage', {
	value: {
		getItem: (key: string) => ls.get(key) ?? null,
		setItem: (key: string, value: string) => ls.set(key, value),
		removeItem: (key: string) => ls.delete(key),
		clear: () => ls.clear(),
	},
	writable: true,
	configurable: true,
});

// ─── Tests ──────────────────────────────────────────────────────

type ThemeStore = (typeof import('./themeStore.svelte'))['themeStore'];

describe('themeStore: theme switching', () => {
	let themeStore: ThemeStore;

	beforeAll(async () => {
		({ themeStore } = await import('./themeStore.svelte'));
	});

	const dataTheme = () => document.documentElement.getAttribute('data-theme');

	it('applies the default theme to <html> on load', () => {
		expect(themeStore.current).toBe('light');
		expect(dataTheme()).toBe('light');
	});

	it('set() updates data-theme and persists to localStorage', () => {
		themeStore.set('dark');
		expect(themeStore.current).toBe('dark');
		expect(dataTheme()).toBe('dark');
		expect(localStorage.getItem('zk-theme')).toBe('dark');

		themeStore.set('slate');
		expect(dataTheme()).toBe('slate');
		expect(localStorage.getItem('zk-theme')).toBe('slate');
	});

	it('toggle() flips between light and dark', () => {
		themeStore.set('light');
		themeStore.toggle();
		expect(themeStore.current).toBe('dark');
		expect(dataTheme()).toBe('dark');

		themeStore.toggle();
		expect(themeStore.current).toBe('light');
		expect(dataTheme()).toBe('light');
	});

	it('cycle() advances light → dark → slate → light', () => {
		themeStore.set('light');
		themeStore.cycle();
		expect(themeStore.current).toBe('dark');
		themeStore.cycle();
		expect(themeStore.current).toBe('slate');
		themeStore.cycle();
		expect(themeStore.current).toBe('light');
	});
});
