<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import type { Note } from '$lib/types';

	interface Props {
		textarea: HTMLTextAreaElement | null;
		onInsert: (linkText: string) => void;
	}

	const { textarea, onInsert }: Props = $props();

	let visible = $state(false);
	let query = $state('');
	let posX = $state(0);
	let posY = $state(0);
	let selectedIndex = $state(0);

	const suggestions = $derived.by(() => {
		if (!query) return [];
		const q = query.toLowerCase();
		return noteStore.notes
			.filter((n) => n.title.toLowerCase().includes(q) || n.id.toLowerCase().includes(q))
			.slice(0, 8);
	});

	/** Watch for [[ trigger in textarea input. */
	export function handleInput() {
		if (!textarea) return;
		const val = textarea.value;
		const pos = textarea.selectionStart;

		// Look back from cursor for an unmatched [[
		const before = val.slice(0, pos);
		const lastOpen = before.lastIndexOf('[[');
		const lastClose = before.lastIndexOf(']]');

		if (lastOpen > lastClose && lastOpen >= 0) {
			query = before.slice(lastOpen + 2);
			visible = true;
			selectedIndex = 0;

			// Position the dropdown near the textarea cursor
			const rect = textarea.getBoundingClientRect();
			posX = rect.left + 16;
			posY = rect.top + 28;
		} else {
			visible = false;
			query = '';
		}
	}

	/** Handle keyboard navigation in autocomplete. */
	export function handleKeydown(e: KeyboardEvent): boolean {
		if (!visible || suggestions.length === 0) return false;

		if (e.key === 'ArrowDown') {
			e.preventDefault();
			selectedIndex = (selectedIndex + 1) % suggestions.length;
			return true;
		}
		if (e.key === 'ArrowUp') {
			e.preventDefault();
			selectedIndex = (selectedIndex - 1 + suggestions.length) % suggestions.length;
			return true;
		}
		if (e.key === 'Enter' || e.key === 'Tab') {
			e.preventDefault();
			insertSuggestion(suggestions[selectedIndex]);
			return true;
		}
		if (e.key === 'Escape') {
			e.preventDefault();
			visible = false;
			return true;
		}
		return false;
	}

	function insertSuggestion(note: Note) {
		if (!textarea) return;
		const val = textarea.value;
		const pos = textarea.selectionStart;
		const before = val.slice(0, pos);
		const lastOpen = before.lastIndexOf('[[');

		const linkText = note.title || note.id;
		const replacement = `[[${linkText}]]`;
		const newVal = val.slice(0, lastOpen) + replacement + val.slice(pos);

		onInsert(newVal);
		visible = false;
		query = '';

		// Restore cursor after the inserted link
		requestAnimationFrame(() => {
			if (!textarea) return;
			const newPos = lastOpen + replacement.length;
			textarea.selectionStart = newPos;
			textarea.selectionEnd = newPos;
			textarea.focus();
		});
	}
</script>

{#if visible && suggestions.length > 0}
	<div
		class="fixed z-50 bg-base-100 border border-base-300 rounded-lg shadow-xl max-h-48 overflow-y-auto w-64"
		style="left: {posX}px; top: {posY}px;"
	>
		{#each suggestions as note, i}
			<button
				class="w-full text-left px-3 py-2 text-sm transition-colors"
				class:bg-primary={i === selectedIndex}
				class:text-primary-content={i === selectedIndex}
				class:hover:bg-base-200={i !== selectedIndex}
				onmouseenter={() => (selectedIndex = i)}
				onclick={() => insertSuggestion(note)}
			>
				<div class="font-medium truncate">{note.title || 'Untitled'}</div>
				<div class="text-xs opacity-60 truncate">{note.id}</div>
			</button>
		{/each}
	</div>
{/if}
