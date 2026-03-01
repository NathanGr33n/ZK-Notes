<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import { fade } from 'svelte/transition';

	let visible = $state(false);
	let previewNote = $state<{ title: string; snippet: string; id: string } | null>(null);
	let posX = $state(0);
	let posY = $state(0);
	let hideTimer: ReturnType<typeof setTimeout> | null = null;

	/** Attach hover listeners to a container with [data-note-link] elements. */
	export function attach(container: HTMLElement) {
		container.addEventListener('mouseenter', handleEnter, true);
		container.addEventListener('mouseleave', handleLeave, true);
		return () => {
			container.removeEventListener('mouseenter', handleEnter, true);
			container.removeEventListener('mouseleave', handleLeave, true);
		};
	}

	function handleEnter(e: Event) {
		const target = (e.target as HTMLElement).closest('[data-note-link]') as HTMLElement | null;
		if (!target) return;

		if (hideTimer) clearTimeout(hideTimer);

		const noteRef = decodeURIComponent(target.dataset.noteLink ?? '');
		const found = noteStore.getById(noteRef)
			?? noteStore.notes.find((n) => n.title.toLowerCase() === noteRef.toLowerCase());

		if (!found) return;

		previewNote = {
			title: found.title || found.id,
			snippet: found.content.slice(0, 150) + (found.content.length > 150 ? '…' : ''),
			id: found.id,
		};

		const rect = target.getBoundingClientRect();
		posX = rect.left;
		posY = rect.bottom + 4;
		visible = true;
	}

	function handleLeave(e: Event) {
		const target = (e.target as HTMLElement).closest('[data-note-link]');
		if (!target) return;
		hideTimer = setTimeout(() => {
			visible = false;
			previewNote = null;
		}, 200);
	}
</script>

{#if visible && previewNote}
	<div
		role="tooltip"
		class="fixed z-50 w-72 bg-base-100 border border-base-300 rounded-lg shadow-xl p-3"
		style="left: {posX}px; top: {posY}px;"
		transition:fade={{ duration: 100 }}
		onmouseenter={() => { if (hideTimer) clearTimeout(hideTimer); }}
		onmouseleave={() => { visible = false; previewNote = null; }}
	>
		<div class="text-sm font-semibold mb-1">{previewNote.title}</div>
		<p class="text-xs opacity-60 leading-relaxed">{previewNote.snippet || 'Empty note'}</p>
		<div class="text-xs opacity-30 mt-1 font-mono">{previewNote.id}</div>
	</div>
{/if}
