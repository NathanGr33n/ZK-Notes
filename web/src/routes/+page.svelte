<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import NoteCard from '$lib/components/NoteCard.svelte';

	let filterType = $state<string>('all');
	let searchQuery = $state('');

	const filtered = $derived.by(() => {
		let list = filterType === 'all'
			? noteStore.notes
			: noteStore.notes.filter((n) => n.type === filterType);

		if (searchQuery.trim()) {
			list = noteStore.search(searchQuery).filter(
				(n) => filterType === 'all' || n.type === filterType
			);
		}

		return list.slice().sort((a, b) => b.lastEdit.localeCompare(a.lastEdit));
	});
</script>

<div class="space-y-4">
	<!-- Toolbar -->
	<div class="flex items-center gap-3 flex-wrap">
		<h2 class="text-lg font-semibold flex-shrink-0">All Notes</h2>

		<!-- Type filter tabs -->
		<div class="tabs tabs-boxed tabs-sm">
			<button class="tab" class:tab-active={filterType === 'all'} onclick={() => (filterType = 'all')}>All</button>
			<button class="tab" class:tab-active={filterType === 'permanent'} onclick={() => (filterType = 'permanent')}>Permanent</button>
			<button class="tab" class:tab-active={filterType === 'literature'} onclick={() => (filterType = 'literature')}>Literature</button>
			<button class="tab" class:tab-active={filterType === 'fleeting'} onclick={() => (filterType = 'fleeting')}>Fleeting</button>
		</div>

		<div class="flex-1"></div>

		<span class="text-xs opacity-50">{filtered.length} notes</span>
	</div>

	<!-- Bento grid -->
	{#if filtered.length === 0}
		<div class="flex flex-col items-center justify-center py-20 opacity-50">
			<p class="text-lg mb-2">No notes yet</p>
			<p class="text-sm">Create your first note using the sidebar.</p>
		</div>
	{:else}
		<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
			{#each filtered as note (note.id)}
				<NoteCard {note} />
			{/each}
		</div>
	{/if}
</div>
