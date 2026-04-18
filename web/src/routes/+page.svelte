<script lang="ts">
	import { goto } from '$app/navigation';
	import { noteStore } from '$lib/noteStore.svelte';
	import type { NoteType, SortMode } from '$lib/types';
	type ViewFilter = 'all' | 'pinned' | NoteType;

	let sortMode = $state<SortMode>('lastEdit');
	let filterMode = $state<ViewFilter>('all');
	let query = $state('');

	const displayNotes = $derived.by(() => {
		const normalizedQuery = query.trim().toLowerCase();
		const filtered = noteStore.activeNotes.filter((note) => {
			if (filterMode === 'pinned' && !note.pinned) return false;
			if (filterMode !== 'all' && filterMode !== 'pinned' && note.type !== filterMode) return false;
			if (!normalizedQuery) return true;

			const haystack = [
				note.id,
				note.title,
				note.type,
				note.tags.join(' '),
				note.content.slice(0, 280),
			].join(' ').toLowerCase();
			return haystack.includes(normalizedQuery);
		});
		return noteStore.sorted(filtered, sortMode);
	});

	function createPage() {
		const note = noteStore.add('standard');
		goto(`/note/${note.id}`);
	}

	function formatDate(iso: string): string {
		const date = new Date(iso);
		return `${date.toLocaleDateString()} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
	}
</script>

<div class="space-y-5">
	<header class="flex items-end justify-between gap-3 flex-wrap">
		<div>
			<p class="text-xs uppercase tracking-wide opacity-45 font-medium">Workspace</p>
			<h1 class="text-2xl font-semibold tracking-tight mt-1">Notes</h1>
			<p class="text-sm opacity-55 mt-1">{displayNotes.length} page{displayNotes.length === 1 ? '' : 's'}</p>
		</div>

		<div class="flex items-center gap-2">
			<input
				type="search"
				class="input input-sm input-bordered w-52 text-xs"
				placeholder="Filter pages"
				value={query}
				oninput={(e) => (query = e.currentTarget.value)}
			/>
			<select
				class="select select-sm select-bordered text-xs"
				value={filterMode}
				onchange={(e) => (filterMode = e.currentTarget.value as ViewFilter)}
			>
				<option value="all">All types</option>
				<option value="pinned">Pinned only</option>
				<option value="permanent">Permanent</option>
				<option value="literature">Literature</option>
				<option value="fleeting">Fleeting</option>
				<option value="standard">Standard</option>
			</select>
			<select
				class="select select-sm select-bordered text-xs"
				value={sortMode}
				onchange={(e) => (sortMode = e.currentTarget.value as SortMode)}
			>
				<option value="lastEdit">Last edited</option>
				<option value="created">Date created</option>
				<option value="alpha">Alphabetical</option>
			</select>
			<button class="btn btn-sm" onclick={createPage}>+ New page</button>
		</div>
	</header>

	{#if displayNotes.length === 0 && noteStore.activeNotes.length === 0}
		<div class="surface p-12 text-center">
			<p class="text-xl font-medium mb-2">No pages yet</p>
			<p class="text-sm opacity-55 mb-5">Create your first page to start building your notes workspace.</p>
			<button class="btn btn-sm" onclick={createPage}>Create page</button>
		</div>
	{:else if displayNotes.length === 0}
		<div class="surface p-10 text-center">
			<p class="text-lg font-medium mb-1">No pages match current filters</p>
			<p class="text-sm opacity-55 mb-4">Adjust filter settings or search query to see results.</p>
			<button
				class="btn btn-sm btn-ghost"
				onclick={() => {
					query = '';
					filterMode = 'all';
				}}
			>
				Clear filters
			</button>
		</div>
	{:else}
		<div class="surface overflow-hidden">
			<div class="grid grid-cols-[minmax(0,1fr)_165px_120px] px-4 py-2 text-[11px] uppercase tracking-wide opacity-45 font-semibold">
				<span>Title</span>
				<span>Last edited</span>
				<span>Type</span>
			</div>
			{#each displayNotes as note}
				<a
					href="/note/{note.id}"
					class="grid grid-cols-[minmax(0,1fr)_165px_120px] border-t border-base-300/75 notion-surface-hover"
				>
					<div class="min-w-0 px-4 py-2.5">
						<div class="truncate text-sm font-medium">{note.title || 'Untitled'}</div>
						<div class="mt-0.5 flex items-center gap-2 text-xs opacity-45 min-w-0">
							<span class="font-mono">{note.id}</span>
							{#if note.tags.length > 0}
								<span class="truncate">#{note.tags.join(' #')}</span>
							{/if}
						</div>
					</div>
					<div class="px-4 py-2.5 text-xs opacity-65 flex items-center">{formatDate(note.lastEdit)}</div>
					<div class="px-4 py-2.5 text-xs opacity-75 capitalize flex items-center">{note.type}</div>
				</a>
			{/each}
		</div>
	{/if}
</div>
