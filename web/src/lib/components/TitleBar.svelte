<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	interface Props {
		onToggleSidebar: () => void;
		onOpenSearch: () => void;
	}

	const { onToggleSidebar, onOpenSearch }: Props = $props();
	const isElectron = typeof window !== 'undefined' && !!window.electronAPI?.isElectron;
	let isMaximized = $state(false);
	let cleanupMaximizedListener: (() => void) | null = null;

	function minimize() {
		window.electronAPI?.minimize();
	}

	function maximize() {
		window.electronAPI?.maximize();
	}

	function close() {
		window.electronAPI?.close();
	}

	onMount(() => {
		if (!window.electronAPI) return;

		window.electronAPI
			.isMaximized()
			.then((value) => { isMaximized = value; })
			.catch(() => { isMaximized = false; });

		cleanupMaximizedListener = window.electronAPI.onMaximizedChanged((value) => {
			isMaximized = value;
		});
	});

	onDestroy(() => {
		cleanupMaximizedListener?.();
	});
</script>

<header class="titlebar">
	<div class="titlebar-row">
		<div class="titlebar-left">
			<button
				class="titlebar-icon-btn"
				onclick={onToggleSidebar}
				aria-label="Toggle sidebar"
				title="Toggle sidebar"
			>
				<svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
					<path d="M2 4h12M2 8h12M2 12h12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
				</svg>
			</button>
			<div class="titlebar-brand">
				<span class="titlebar-brand-dot" aria-hidden="true"></span>
				<span class="titlebar-brand-name">ZK Notes</span>
			</div>
		</div>

		<button class="titlebar-search" onclick={onOpenSearch} title="Search (Ctrl+K)">
			<svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
				<circle cx="7" cy="7" r="5" stroke="currentColor" stroke-width="1.5" />
				<path d="M11 11l3.5 3.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
			</svg>
			<span>Search</span>
			<kbd>Ctrl+K</kbd>
		</button>

		<div class="titlebar-right">
			{#if isElectron}
				<button class="window-btn" onclick={minimize} aria-label="Minimize">
					<svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
						<path d="M2 6h8" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" />
					</svg>
				</button>
				<button class="window-btn" onclick={maximize} aria-label={isMaximized ? 'Restore' : 'Maximize'}>
					{#if isMaximized}
						<svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
							<path d="M4 2.2h5.8v5.8H9" stroke="currentColor" stroke-width="1.1" fill="none" />
							<rect x="2.2" y="4" width="5.8" height="5.8" rx="0.8" stroke="currentColor" stroke-width="1.1" fill="none" />
						</svg>
					{:else}
						<svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
							<rect x="2.25" y="2.25" width="7.5" height="7.5" rx="1" stroke="currentColor" stroke-width="1.2" fill="none" />
						</svg>
					{/if}
				</button>
				<button class="window-btn window-btn-close" onclick={close} aria-label="Close">
					<svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
						<path d="M3 3l6 6M9 3l-6 6" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" />
					</svg>
				</button>
			{/if}
		</div>
	</div>
</header>

<style>
	.titlebar {
		height: 40px;
		flex-shrink: 0;
		background: oklch(var(--b2));
		border-bottom: 1px solid oklch(var(--b3) / 0.75);
		user-select: none;
		z-index: 40;
	}

	.titlebar-row {
		display: flex;
		align-items: center;
		gap: 10px;
		height: 100%;
		padding: 0 8px;
		-webkit-app-region: drag;
	}

	.titlebar-left {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
		-webkit-app-region: no-drag;
	}

	.titlebar-icon-btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		border-radius: 6px;
		border: none;
		background: transparent;
		color: oklch(var(--bc) / 0.62);
		cursor: pointer;
		transition: background-color 0.12s ease, color 0.12s ease;
	}

	.titlebar-icon-btn:hover {
		background: oklch(var(--b3) / 0.6);
		color: oklch(var(--bc) / 0.92);
	}

	.titlebar-brand {
		display: flex;
		align-items: center;
		gap: 6px;
		color: oklch(var(--bc) / 0.74);
	}

	.titlebar-brand-dot {
		width: 8px;
		height: 8px;
		border-radius: 999px;
		background: oklch(var(--p) / 0.75);
	}

	.titlebar-brand-name {
		font-size: 0.78rem;
		font-weight: 600;
		letter-spacing: 0.01em;
	}

	.titlebar-search {
		display: inline-flex;
		align-items: center;
		gap: 8px;
		height: 28px;
		min-width: 0;
		margin: 0 auto;
		padding: 0 10px;
		border-radius: 7px;
		border: 1px solid oklch(var(--b3) / 0.8);
		background: oklch(var(--b1));
		color: oklch(var(--bc) / 0.62);
		font-size: 0.76rem;
		cursor: pointer;
		transition: border-color 0.12s ease, color 0.12s ease, background-color 0.12s ease;
		-webkit-app-region: no-drag;
	}

	.titlebar-search:hover {
		background: oklch(var(--b2));
		border-color: oklch(var(--b3));
		color: oklch(var(--bc) / 0.9);
	}

	.titlebar-search span {
		white-space: nowrap;
	}

	.titlebar-right {
		display: flex;
		align-items: center;
		gap: 2px;
		margin-left: auto;
		-webkit-app-region: no-drag;
	}

	.window-btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 32px;
		height: 28px;
		border: none;
		border-radius: 6px;
		background: transparent;
		color: oklch(var(--bc) / 0.62);
		cursor: pointer;
		transition: background-color 0.12s ease, color 0.12s ease;
	}

	.window-btn:hover {
		background: oklch(var(--b3) / 0.7);
		color: oklch(var(--bc));
	}

	.window-btn-close:hover {
		background: rgba(200, 42, 42, 0.86);
		color: #fff;
	}
</style>
