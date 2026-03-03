<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import NoteCard from '$lib/components/NoteCard.svelte';
	import type { SortMode } from '$lib/types';

	let sortMode = $state<SortMode>('lastEdit');

	const displayNotes = $derived.by(() => {
		return noteStore.sorted(noteStore.activeNotes, sortMode);
	});

	const stats = $derived.by(() => ({
		total: noteStore.activeNotes.length,
		permanent: noteStore.activeNotes.filter((n) => n.type === 'permanent').length,
		literature: noteStore.activeNotes.filter((n) => n.type === 'literature').length,
		fleeting: noteStore.activeNotes.filter((n) => n.type === 'fleeting').length,
		pinned: noteStore.pinnedNotes.length,
	}));
</script>

<div class="space-y-6">
	<!-- Stats bar -->
	<div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
		<div class="surface p-4">
			<div class="text-2xl font-bold">{stats.total}</div>
			<div class="text-xs opacity-40 mt-1">Total Notes</div>
		</div>
		<div class="surface p-4">
			<div class="text-2xl font-bold">{stats.permanent}</div>
			<div class="text-xs opacity-40 mt-1">📝 Permanent</div>
		</div>
		<div class="surface p-4">
			<div class="text-2xl font-bold">{stats.literature}</div>
			<div class="text-xs opacity-40 mt-1">📖 Literature</div>
		</div>
		<div class="surface p-4">
			<div class="text-2xl font-bold">{stats.fleeting}</div>
			<div class="text-xs opacity-40 mt-1">⚡ Fleeting</div>
		</div>
	</div>

	<!-- Sort controls -->
	<div class="flex items-center justify-between">
		<h2 class="text-base font-semibold opacity-80">All Notes</h2>
		<div class="flex items-center gap-2">
			<span class="text-xs opacity-30">{displayNotes.length} notes</span>
			<select
				class="select select-sm select-bordered text-xs"
				value={sortMode}
				onchange={(e) => (sortMode = e.currentTarget.value as SortMode)}
			>
				<option value="lastEdit">Last Edited</option>
				<option value="created">Date Created</option>
				<option value="alpha">Alphabetical</option>
			</select>
		</div>
	</div>

	<!-- Bento Grid -->
	{#if displayNotes.length === 0}
		<div class="flex flex-col items-center justify-center py-24">
			<div class="surface p-8 text-center max-w-md">
				<div class="text-4xl mb-4">📝</div>
				<h3 class="text-lg font-semibold mb-2 opacity-80">Start your Zettelkasten</h3>
				<p class="text-sm opacity-40 mb-6 leading-relaxed">
					Capture atomic ideas, link them together, and let structure emerge.
					Use <kbd class="kbd kbd-sm">Ctrl+Shift+N</kbd> for quick capture.
				</p>
				<div class="flex gap-2 justify-center">
					<button class="btn btn-primary btn-sm" onclick={() => noteStore.add('permanent')}>Create Note</button>
					<button class="btn btn-ghost btn-sm" onclick={() => noteStore.add('fleeting')}>⚡ Quick Note</button>
				</div>
			</div>
		</div>
	{:else}
		<!-- Pinned section -->
		{#if noteStore.pinnedNotes.length > 0}
			<div class="space-y-3">
				<h3 class="text-xs uppercase tracking-wider opacity-40 font-medium">📌 Pinned</h3>
				<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
					{#each noteStore.pinnedNotes as note (note.id)}
						<NoteCard {note} size="featured" />
					{/each}
				</div>
			</div>
		{/if}

		<!-- All notes bento grid -->
		<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
			{#each displayNotes as note, i (note.id)}
				<NoteCard {note} size={i === 0 && !note.pinned ? 'featured' : 'normal'} />
			{/each}
		</div>
	{/if}
</div>
