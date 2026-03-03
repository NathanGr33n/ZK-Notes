<script lang="ts">
	interface Props {
		onToggleSidebar: () => void;
		onOpenSearch: () => void;
	}

	const { onToggleSidebar, onOpenSearch }: Props = $props();

	const isElectron = typeof window !== 'undefined' && !!(window as any).electronAPI?.isElectron;

	function minimize() {
		(window as any).electronAPI?.minimize();
	}
	function maximize() {
		(window as any).electronAPI?.maximize();
	}
	function close() {
		(window as any).electronAPI?.close();
	}
</script>

<header class="titlebar">
	<div class="titlebar-drag">
		<!-- Left: sidebar toggle + branding -->
		<div class="titlebar-left">
			<button
				class="titlebar-btn"
				onclick={onToggleSidebar}
				aria-label="Toggle sidebar"
			>
				<svg width="16" height="16" viewBox="0 0 16 16" fill="none">
					<path d="M2 4h12M2 8h12M2 12h12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
				</svg>
			</button>
			<div class="titlebar-brand">
				<span class="titlebar-logo">Z</span>
				<span class="titlebar-name">ZK-Notes</span>
			</div>
		</div>

		<!-- Center: search trigger -->
		<button class="titlebar-search" onclick={onOpenSearch}>
			<svg width="14" height="14" viewBox="0 0 16 16" fill="none">
				<circle cx="7" cy="7" r="5" stroke="currentColor" stroke-width="1.5"/>
				<path d="M11 11l3.5 3.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
			</svg>
			<span>Search notes...</span>
			<kbd>Ctrl+K</kbd>
		</button>

		<!-- Right: window controls -->
		<div class="titlebar-right">
			{#if isElectron}
				<button class="window-btn" onclick={minimize} aria-label="Minimize">
					<svg width="12" height="12" viewBox="0 0 12 12">
						<path d="M2 6h8" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
					</svg>
				</button>
				<button class="window-btn" onclick={maximize} aria-label="Maximize">
					<svg width="12" height="12" viewBox="0 0 12 12">
						<rect x="2" y="2" width="8" height="8" rx="1" stroke="currentColor" stroke-width="1.2" fill="none"/>
					</svg>
				</button>
				<button class="window-btn window-btn-close" onclick={close} aria-label="Close">
					<svg width="12" height="12" viewBox="0 0 12 12">
						<path d="M3 3l6 6M9 3l-6 6" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
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
		background: var(--titlebar-bg, rgba(13, 13, 13, 0.95));
		border-bottom: 1px solid rgba(255, 255, 255, 0.06);
		backdrop-filter: blur(12px);
		-webkit-backdrop-filter: blur(12px);
		user-select: none;
		z-index: 100;
	}

	.titlebar-drag {
		display: flex;
		align-items: center;
		height: 100%;
		padding: 0 8px;
		-webkit-app-region: drag;
	}

	.titlebar-left {
		display: flex;
		align-items: center;
		gap: 8px;
		-webkit-app-region: no-drag;
	}

	.titlebar-btn {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		border: none;
		background: transparent;
		color: rgba(255, 255, 255, 0.5);
		border-radius: 6px;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.titlebar-btn:hover {
		background: rgba(255, 255, 255, 0.08);
		color: rgba(255, 255, 255, 0.8);
	}

	.titlebar-brand {
		display: flex;
		align-items: center;
		gap: 6px;
	}

	.titlebar-logo {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 22px;
		height: 22px;
		background: rgba(255, 255, 255, 0.08);
		border-radius: 5px;
		font-size: 11px;
		font-weight: 700;
		color: rgba(255, 255, 255, 0.7);
		letter-spacing: -0.02em;
	}

	.titlebar-name {
		font-size: 12px;
		font-weight: 600;
		color: rgba(255, 255, 255, 0.5);
		letter-spacing: -0.01em;
	}

	.titlebar-search {
		display: flex;
		align-items: center;
		gap: 8px;
		margin: 0 auto;
		padding: 4px 12px;
		height: 28px;
		min-width: 220px;
		background: rgba(255, 255, 255, 0.04);
		border: 1px solid rgba(255, 255, 255, 0.06);
		border-radius: 7px;
		color: rgba(255, 255, 255, 0.35);
		font-size: 12px;
		cursor: pointer;
		transition: all 0.15s ease;
		-webkit-app-region: no-drag;
	}

	.titlebar-search:hover {
		background: rgba(255, 255, 255, 0.06);
		border-color: rgba(255, 255, 255, 0.1);
		color: rgba(255, 255, 255, 0.5);
	}

	.titlebar-search kbd {
		margin-left: auto;
		padding: 1px 5px;
		font-size: 10px;
		background: rgba(255, 255, 255, 0.06);
		border-radius: 4px;
		font-family: inherit;
		color: rgba(255, 255, 255, 0.3);
	}

	.titlebar-right {
		display: flex;
		align-items: center;
		gap: 2px;
		-webkit-app-region: no-drag;
	}

	.window-btn {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 32px;
		height: 28px;
		border: none;
		background: transparent;
		color: rgba(255, 255, 255, 0.4);
		cursor: pointer;
		transition: all 0.1s ease;
		border-radius: 4px;
	}

	.window-btn:hover {
		background: rgba(255, 255, 255, 0.08);
		color: rgba(255, 255, 255, 0.8);
	}

	.window-btn-close:hover {
		background: rgba(232, 65, 65, 0.85);
		color: white;
	}
</style>
