const THEME_KEY = 'zk-theme';
const DENSITY_KEY = 'zk-density';
const MOTION_KEY = 'zk-motion';

export const THEMES = ['light', 'dark', 'slate'] as const;
type Theme = (typeof THEMES)[number];
type Density = 'comfortable' | 'compact';
type MotionMode = 'full' | 'reduced';

function isTheme(value: string | null): value is Theme {
	return !!value && THEMES.includes(value as Theme);
}

function isDensity(value: string | null): value is Density {
	return value === 'comfortable' || value === 'compact';
}

function isMotionMode(value: string | null): value is MotionMode {
	return value === 'full' || value === 'reduced';
}

function persist(key: string, value: string): void {
	try { localStorage.setItem(key, value); } catch { /* noop */ }
}

function createThemeStore() {
	let theme = $state<Theme>(loadTheme());
	let density = $state<Density>(loadDensity());
	let motion = $state<MotionMode>(loadMotionMode());

	function apply() {
		applyTheme(theme);
		applyDensity(density);
		applyMotionMode(motion);
	}

	function toggle() {
		theme = theme === 'dark' ? 'light' : 'dark';
		persist(THEME_KEY, theme);
		apply();
	}

	function cycle() {
		const currentIndex = THEMES.indexOf(theme);
		const nextIndex = (currentIndex + 1) % THEMES.length;
		theme = THEMES[nextIndex];
		persist(THEME_KEY, theme);
		apply();
	}

	function set(t: Theme) {
		theme = t;
		persist(THEME_KEY, theme);
		apply();
	}

	function setDensity(next: Density) {
		density = next;
		persist(DENSITY_KEY, density);
		apply();
	}

	function toggleDensity() {
		setDensity(density === 'comfortable' ? 'compact' : 'comfortable');
	}

	function setMotionMode(next: MotionMode) {
		motion = next;
		persist(MOTION_KEY, motion);
		apply();
	}

	// Apply persisted appearance on initial load. The apply* helpers are
	// SSR-safe and no-op when `document` is undefined.
	apply();

	return {
		get current() { return theme; },
		get density() { return density; },
		get motion() { return motion; },
		get themes() { return THEMES; },
		toggle,
		cycle,
		set,
		setDensity,
		toggleDensity,
		setMotionMode,
	};
}

function loadTheme(): Theme {
	if (typeof localStorage === 'undefined') return 'light';
	const value = localStorage.getItem(THEME_KEY);
	return isTheme(value) ? value : 'light';
}

function loadDensity(): Density {
	if (typeof localStorage === 'undefined') return 'comfortable';
	const value = localStorage.getItem(DENSITY_KEY);
	return isDensity(value) ? value : 'comfortable';
}

function loadMotionMode(): MotionMode {
	if (typeof localStorage === 'undefined') return 'full';
	const value = localStorage.getItem(MOTION_KEY);
	return isMotionMode(value) ? value : 'full';
}

function applyTheme(theme: Theme) {
	if (typeof document === 'undefined') return;
	document.documentElement.setAttribute('data-theme', theme);
}

function applyDensity(density: Density) {
	if (typeof document === 'undefined') return;
	document.documentElement.setAttribute('data-density', density);
}

function applyMotionMode(mode: MotionMode) {
	if (typeof document === 'undefined') return;
	document.documentElement.setAttribute('data-motion', mode);
}

export const themeStore = createThemeStore();
