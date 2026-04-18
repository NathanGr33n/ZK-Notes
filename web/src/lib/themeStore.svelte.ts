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

	// Apply theme reactively whenever it changes
	$effect(() => {
		applyTheme(theme);
	});

	return {
		get current() { return theme; },
		toggle,
		set,
	};
}

function loadTheme(): Theme {
	if (typeof localStorage === 'undefined') return 'light';
	return (localStorage.getItem(THEME_KEY) as Theme) ?? 'light';
}

function applyTheme(theme: Theme) {
	if (typeof document === 'undefined') return;
	document.documentElement.setAttribute('data-theme', theme);
}

export const themeStore = createThemeStore();
