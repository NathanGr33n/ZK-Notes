<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import { goto } from '$app/navigation';
	import { fade, scale } from 'svelte/transition';
	import { onMount, onDestroy } from 'svelte';

	let visible = $state(false);
	let title = $state('');
	let content = $state('');
	let titleInput: HTMLInputElement | null = $state(null);

	function open() {
		visible = true;
		title = '';
		content = '';
		requestAnimationFrame(() => titleInput?.focus());
	}

	function close() {
		visible = false;
		title = '';
		content = '';
	}

	function save() {
		if (!title.trim() && !content.trim()) {
			close();
			return;
		}
		const note = noteStore.add('fleeting');
		noteStore.update(note.id, { title: title.trim(), content: content.trim() });
		close();
	}

	function saveAndOpen() {
		if (!title.trim() && !content.trim()) {
			close();
			return;
		}
		const note = noteStore.add('fleeting');
		noteStore.update(note.id, { title: title.trim(), content: content.trim() });
		close();
		goto(`/note/${note.id}`);
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			e.preventDefault();
			close();
		}
		if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
			e.preventDefault();
			save();
		}
	}

	// Global keyboard listener
	function globalKeydown(e: KeyboardEvent) {
		if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'N') {
			e.preventDefault();
			if (visible) close();
			else open();
		}
	}

	// Listen for Electron IPC quick-capture signal
	let cleanupElectron: (() => void) | null = null;

	onMount(() => {
		document.addEventListener('keydown', globalKeydown);
		const api = (window as any).electronAPI;
		if (api?.onQuickCapture) {
			cleanupElectron = api.onQuickCapture(() => {
				if (visible) close();
				else open();
			});
		}
	});

	onDestroy(() => {
		if (typeof document !== 'undefined') {
			document.removeEventListener('keydown', globalKeydown);
		}
		cleanupElectron?.();
	});
</script>

{#if visible}
	<!-- Backdrop -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<div
		class="fixed inset-0 z-[200] flex items-start justify-center pt-[15vh]"
		transition:fade={{ duration: 100 }}
		onclick={(e) => { if (e.target === e.currentTarget) close(); }}
	>
		<div class="fixed inset-0 bg-black/60 backdrop-blur-sm"></div>

		<!-- Panel -->
		<div
			class="quick-capture-panel"
			transition:scale={{ start: 0.95, duration: 150 }}
			onkeydown={handleKeydown}
			role="dialog"
			tabindex="-1"
			aria-label="Quick capture"
		>
			<div class="quick-capture-header">
				<span class="quick-capture-badge">⚡ Fleeting Note</span>
				<button class="quick-capture-close" onclick={close} aria-label="Close quick capture">
					<svg width="14" height="14" viewBox="0 0 14 14">
						<path d="M3 3l8 8M11 3l-8 8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
					</svg>
				</button>
			</div>

			<input
				bind:this={titleInput}
				type="text"
				class="quick-capture-title"
				placeholder="What's on your mind?"
				bind:value={title}
			/>

			<textarea
				class="quick-capture-content"
				placeholder="Capture your thought... (Ctrl+Enter to save)"
				bind:value={content}
				rows="4"
			></textarea>

			<div class="quick-capture-footer">
				<span class="quick-capture-hint">Ctrl+Enter to save · Esc to discard</span>
				<div class="quick-capture-actions">
					<button class="btn btn-ghost btn-sm" onclick={close}>Discard</button>
					<button class="btn btn-ghost btn-sm" onclick={saveAndOpen}>Save & Open</button>
					<button class="btn btn-primary btn-sm" onclick={save}>Save</button>
				</div>
			</div>
		</div>
	</div>
{/if}

<style>
	.quick-capture-panel {
		position: relative;
		z-index: 1;
		width: 480px;
		max-width: 90vw;
		background: oklch(var(--b1) / 0.98);
		border: 1px solid oklch(var(--b3));
		border-radius: 10px;
		backdrop-filter: blur(14px);
		-webkit-backdrop-filter: blur(14px);
		box-shadow: 0 18px 40px rgba(15, 23, 42, 0.2);
		overflow: hidden;
	}

	.quick-capture-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 12px 16px 0;
	}

	.quick-capture-badge {
		font-size: 11px;
		font-weight: 600;
		color: oklch(var(--bc) / 0.72);
		letter-spacing: 0.02em;
	}

	.quick-capture-close {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 24px;
		height: 24px;
		border: none;
		background: transparent;
		color: oklch(var(--bc) / 0.5);
		border-radius: 4px;
		cursor: pointer;
		transition: all 0.1s ease;
	}

	.quick-capture-close:hover {
		background: oklch(var(--b3) / 0.7);
		color: oklch(var(--bc));
	}

	.quick-capture-title {
		display: block;
		width: 100%;
		padding: 12px 16px 4px;
		background: transparent;
		border: none;
		color: oklch(var(--bc));
		font-size: 16px;
		font-weight: 600;
		outline: none;
		letter-spacing: -0.01em;
	}

	.quick-capture-title::placeholder {
		color: oklch(var(--bc) / 0.38);
	}

	.quick-capture-content {
		display: block;
		width: 100%;
		padding: 4px 16px 12px;
		background: transparent;
		border: none;
		color: oklch(var(--bc) / 0.84);
		font-size: 13px;
		line-height: 1.6;
		outline: none;
		resize: none;
	}

	.quick-capture-content::placeholder {
		color: oklch(var(--bc) / 0.38);
	}

	.quick-capture-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 8px 12px;
		border-top: 1px solid oklch(var(--b3) / 0.85);
		background: oklch(var(--b2) / 0.68);
	}

	.quick-capture-hint {
		font-size: 11px;
		color: oklch(var(--bc) / 0.52);
	}

	.quick-capture-actions {
		display: flex;
		gap: 4px;
	}
</style>
