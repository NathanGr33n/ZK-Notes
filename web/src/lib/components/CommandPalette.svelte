<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import { themeStore } from '$lib/themeStore.svelte';
	import { goto } from '$app/navigation';
	import { fade, scale } from 'svelte/transition';
	import { onMount, onDestroy } from 'svelte';

	let visible = $state(false);
	let query = $state('');
	let selectedIndex = $state(0);
	let inputEl: HTMLInputElement | null = $state(null);

	interface Action {
		id: string;
		label: string;
		hint?: string;
		icon: string;
		action: () => void;
	}

	const staticActions: Action[] = [
		{ id: 'new-permanent', label: 'New Permanent Note', icon: '📝', hint: 'Create', action: () => { noteStore.add('permanent'); close(); } },
		{ id: 'new-fleeting', label: 'New Fleeting Note', icon: '⚡', hint: 'Create', action: () => { noteStore.add('fleeting'); close(); } },
		{ id: 'new-literature', label: 'New Literature Note', icon: '📖', hint: 'Create', action: () => { noteStore.add('literature'); close(); } },
		{ id: 'toggle-theme', label: 'Toggle Theme', icon: '🎨', hint: 'Settings', action: () => { themeStore.toggle(); close(); } },
		{ id: 'go-home', label: 'Go to Workspace', icon: '🏠', hint: 'Navigate', action: () => { goto('/'); close(); } },
		{ id: 'go-trash', label: 'Open Trash', icon: '🗑️', hint: 'Navigate', action: () => { goto('/trash'); close(); } },
	];

	const results = $derived.by<Action[]>(() => {
		const q = query.toLowerCase().trim();
		const noteActions: Action[] = noteStore.activeNotes
			.filter((n) =>
				n.title.toLowerCase().includes(q) ||
				n.id.toLowerCase().includes(q) ||
				n.content.toLowerCase().includes(q)
			)
			.slice(0, 8)
			.map((n) => ({
				id: n.id,
				label: n.title || n.id,
				hint: n.type,
				icon: n.type === 'fleeting' ? '⚡' : n.type === 'literature' ? '📖' : '📝',
				action: () => { goto(`/note/${n.id}`); close(); },
			}));

		const filteredStatic = q
			? staticActions.filter((a) => a.label.toLowerCase().includes(q))
			: staticActions;

		return [...noteActions, ...filteredStatic].slice(0, 12);
	});

	export function open() {
		visible = true;
		query = '';
		selectedIndex = 0;
		requestAnimationFrame(() => inputEl?.focus());
	}

	function close() {
		visible = false;
		query = '';
		selectedIndex = 0;
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			e.preventDefault();
			close();
			return;
		}
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			selectedIndex = (selectedIndex + 1) % Math.max(results.length, 1);
			return;
		}
		if (e.key === 'ArrowUp') {
			e.preventDefault();
			selectedIndex = (selectedIndex - 1 + results.length) % Math.max(results.length, 1);
			return;
		}
		if (e.key === 'Enter') {
			e.preventDefault();
			const item = results[selectedIndex];
			if (item) item.action();
			return;
		}
	}

	function globalKeydown(e: KeyboardEvent) {
		if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
			e.preventDefault();
			if (visible) close();
			else open();
		}
	}

	onMount(() => {
		document.addEventListener('keydown', globalKeydown);
	});

	onDestroy(() => {
		if (typeof document !== 'undefined') {
			document.removeEventListener('keydown', globalKeydown);
		}
	});

	// Reset selection when query changes
	$effect(() => {
		query;
		selectedIndex = 0;
	});
</script>

{#if visible}
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<div
		class="fixed inset-0 z-[200] flex items-start justify-center pt-[12vh]"
		transition:fade={{ duration: 80 }}
		onclick={(e) => { if (e.target === e.currentTarget) close(); }}
	>
		<div class="fixed inset-0 bg-black/50 backdrop-blur-sm"></div>

		<div
			class="palette-panel"
			transition:scale={{ start: 0.97, duration: 120 }}
			onkeydown={handleKeydown}
			role="dialog"
			tabindex="-1"
			aria-label="Command palette"
		>
			<!-- Search input -->
			<div class="palette-input-row">
				<svg width="16" height="16" viewBox="0 0 16 16" fill="none" class="palette-search-icon">
					<circle cx="7" cy="7" r="5" stroke="currentColor" stroke-width="1.5"/>
					<path d="M11 11l3.5 3.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
				</svg>
				<input
					bind:this={inputEl}
					type="text"
					class="palette-input"
					placeholder="Search notes or type a command..."
					bind:value={query}
				/>
			</div>

			<!-- Results -->
			{#if results.length > 0}
				<div class="palette-results">
					{#each results as item, i}
						<button
							class="palette-item"
							class:palette-item-active={i === selectedIndex}
							onmouseenter={() => (selectedIndex = i)}
							onclick={() => item.action()}
						>
							<span class="palette-item-icon">{item.icon}</span>
							<span class="palette-item-label">{item.label}</span>
							{#if item.hint}
								<span class="palette-item-hint">{item.hint}</span>
							{/if}
						</button>
					{/each}
				</div>
			{:else if query.trim()}
				<div class="palette-empty">No results found</div>
			{/if}

			<div class="palette-footer">
				<span>↑↓ Navigate</span>
				<span>↵ Open</span>
				<span>Esc Close</span>
			</div>
		</div>
	</div>
{/if}

<style>
	.palette-panel {
		position: relative;
		z-index: 1;
		width: 520px;
		max-width: 90vw;
		background: rgba(18, 18, 18, 0.96);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: 12px;
		backdrop-filter: blur(24px);
		-webkit-backdrop-filter: blur(24px);
		box-shadow: 0 24px 48px rgba(0, 0, 0, 0.5);
		overflow: hidden;
	}

	.palette-input-row {
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 12px 16px;
		border-bottom: 1px solid rgba(255, 255, 255, 0.06);
	}

	.palette-search-icon {
		flex-shrink: 0;
		color: rgba(255, 255, 255, 0.3);
	}

	.palette-input {
		flex: 1;
		background: transparent;
		border: none;
		color: rgba(255, 255, 255, 0.9);
		font-size: 14px;
		outline: none;
	}

	.palette-input::placeholder {
		color: rgba(255, 255, 255, 0.25);
	}

	.palette-results {
		max-height: 320px;
		overflow-y: auto;
		padding: 4px;
	}

	.palette-item {
		display: flex;
		align-items: center;
		gap: 10px;
		width: 100%;
		padding: 8px 12px;
		border: none;
		background: transparent;
		border-radius: 8px;
		color: rgba(255, 255, 255, 0.7);
		font-size: 13px;
		cursor: pointer;
		text-align: left;
		transition: background 0.08s ease;
	}

	.palette-item-active {
		background: rgba(255, 255, 255, 0.06);
		color: rgba(255, 255, 255, 0.95);
	}

	.palette-item-icon {
		flex-shrink: 0;
		font-size: 14px;
	}

	.palette-item-label {
		flex: 1;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.palette-item-hint {
		flex-shrink: 0;
		font-size: 11px;
		color: rgba(255, 255, 255, 0.25);
		text-transform: capitalize;
	}

	.palette-empty {
		padding: 24px;
		text-align: center;
		color: rgba(255, 255, 255, 0.25);
		font-size: 13px;
	}

	.palette-footer {
		display: flex;
		gap: 16px;
		padding: 8px 16px;
		border-top: 1px solid rgba(255, 255, 255, 0.06);
		background: rgba(255, 255, 255, 0.02);
	}

	.palette-footer span {
		font-size: 11px;
		color: rgba(255, 255, 255, 0.2);
	}
</style>
