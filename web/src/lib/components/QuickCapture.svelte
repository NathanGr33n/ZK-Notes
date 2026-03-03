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
		background: rgba(20, 20, 20, 0.95);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: 12px;
		backdrop-filter: blur(24px);
		-webkit-backdrop-filter: blur(24px);
		box-shadow: 0 24px 48px rgba(0, 0, 0, 0.5), 0 0 0 1px rgba(255, 255, 255, 0.03) inset;
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
		color: oklch(65% 0.16 80);
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
		color: rgba(255, 255, 255, 0.3);
		border-radius: 4px;
		cursor: pointer;
		transition: all 0.1s ease;
	}

	.quick-capture-close:hover {
		background: rgba(255, 255, 255, 0.08);
		color: rgba(255, 255, 255, 0.6);
	}

	.quick-capture-title {
		display: block;
		width: 100%;
		padding: 12px 16px 4px;
		background: transparent;
		border: none;
		color: rgba(255, 255, 255, 0.9);
		font-size: 16px;
		font-weight: 600;
		outline: none;
		letter-spacing: -0.01em;
	}

	.quick-capture-title::placeholder {
		color: rgba(255, 255, 255, 0.25);
	}

	.quick-capture-content {
		display: block;
		width: 100%;
		padding: 4px 16px 12px;
		background: transparent;
		border: none;
		color: rgba(255, 255, 255, 0.7);
		font-size: 13px;
		line-height: 1.6;
		outline: none;
		resize: none;
	}

	.quick-capture-content::placeholder {
		color: rgba(255, 255, 255, 0.2);
	}

	.quick-capture-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 8px 12px;
		border-top: 1px solid rgba(255, 255, 255, 0.06);
		background: rgba(255, 255, 255, 0.02);
	}

	.quick-capture-hint {
		font-size: 11px;
		color: rgba(255, 255, 255, 0.2);
	}

	.quick-capture-actions {
		display: flex;
		gap: 4px;
	}
</style>
