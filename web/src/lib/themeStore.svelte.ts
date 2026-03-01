const THEME_KEY = 'zk-theme';
type Theme = 'light' | 'dark';

function createThemeStore() {
	let theme = $state<Theme>(loadTheme());

	function toggle() {
		theme = theme === 'light' ? 'dark' : 'light';
		applyTheme(theme);
		try { localStorage.setItem(THEME_KEY, theme); } catch { /* noop */ }
	}

	function set(t: Theme) {
		theme = t;
		applyTheme(theme);
		try { localStorage.setItem(THEME_KEY, theme); } catch { /* noop */ }
	}

	// Apply on init (client-side only)
	if (typeof document !== 'undefined') {
		applyTheme(theme);
	}

	return {
		get current() { return theme; },
		toggle,
		set,
	};
}

function loadTheme(): Theme {
	if (typeof localStorage === 'undefined') return 'dark';
	return (localStorage.getItem(THEME_KEY) as Theme) ?? 'dark';
}

function applyTheme(theme: Theme) {
	if (typeof document === 'undefined') return;
	document.documentElement.setAttribute('data-theme', theme);
}

export const themeStore = createThemeStore();
