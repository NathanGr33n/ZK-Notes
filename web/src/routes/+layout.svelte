<script lang="ts">
	import '../app.css';
	import { themeStore } from '$lib/themeStore.svelte';
	import { noteStore } from '$lib/noteStore.svelte';
	import { slide } from 'svelte/transition';
	import { onMount } from 'svelte';
	import TitleBar from '$lib/components/TitleBar.svelte';
	import CommandPalette from '$lib/components/CommandPalette.svelte';
	import QuickCapture from '$lib/components/QuickCapture.svelte';

	let { children } = $props();

	onMount(() => {
		noteStore.init();
	});

	let sidebarOpen = $state(true);
	let noteTypeFilter = $state<'all' | 'permanent' | 'literature' | 'fleeting'>('all');
	let tagFilter = $state<string | null>(null);

	let commandPalette: CommandPalette | null = $state(null);

	function openPalette() {
		commandPalette?.open();
	}

	const recentNotes = $derived.by(() =>
		noteStore.activeNotes
			.slice()
			.sort((a, b) => b.lastEdit.localeCompare(a.lastEdit))
			.slice(0, 10)
	);

	const filteredActive = $derived.by(() => {
		let list = noteStore.activeNotes;
		if (noteTypeFilter !== 'all') list = list.filter((n) => n.type === noteTypeFilter);
	if (tagFilter) list = list.filter((n) => n.tags.includes(tagFilter!));
		return list;
	});
</script>

<CommandPalette bind:this={commandPalette} />
<QuickCapture />

<div class="flex flex-col h-screen overflow-hidden bg-base-100 text-base-content">
	<TitleBar onToggleSidebar={() => (sidebarOpen = !sidebarOpen)} onOpenSearch={openPalette} />

	<div class="flex flex-1 overflow-hidden">
		<!-- Sidebar -->
		{#if sidebarOpen}
			<aside
				transition:slide={{ axis: 'x', duration: 160 }}
				class="w-72 flex-shrink-0 border-r border-base-300 bg-base-200/40 backdrop-blur-xl flex flex-col"
			>
				<div class="p-3">
					<!-- Create -->
					<div class="space-y-2">
						<button class="btn btn-primary btn-sm w-full" onclick={() => noteStore.add('permanent')}>
							New Permanent Note
						</button>
						<div class="grid grid-cols-2 gap-2">
							<button class="btn btn-ghost btn-sm" onclick={() => noteStore.add('fleeting')}>
								⚡ Fleeting
							</button>
							<button class="btn btn-ghost btn-sm" onclick={() => noteStore.add('literature')}>
								📖 Literature
							</button>
						</div>
					</div>

					<div class="divider my-3"></div>

					<!-- Filters -->
					<div class="space-y-2">
						<div class="text-[11px] tracking-wide uppercase opacity-50 px-1">Filters</div>
						<div class="grid grid-cols-2 gap-2">
							<button class="btn btn-ghost btn-sm" class:btn-active={noteTypeFilter === 'all'} onclick={() => (noteTypeFilter = 'all')}>All</button>
							<button class="btn btn-ghost btn-sm" class:btn-active={noteTypeFilter === 'permanent'} onclick={() => (noteTypeFilter = 'permanent')}>Permanent</button>
							<button class="btn btn-ghost btn-sm" class:btn-active={noteTypeFilter === 'literature'} onclick={() => (noteTypeFilter = 'literature')}>Literature</button>
							<button class="btn btn-ghost btn-sm" class:btn-active={noteTypeFilter === 'fleeting'} onclick={() => (noteTypeFilter = 'fleeting')}>Fleeting</button>
						</div>
						{#if tagFilter}
							<div class="flex items-center justify-between px-1">
								<span class="text-xs opacity-60">Tag: #{tagFilter}</span>
								<button class="btn btn-ghost btn-xs" onclick={() => (tagFilter = null)}>Clear</button>
							</div>
						{/if}
					</div>
				</div>

				<!-- Navigation Sections -->
				<nav class="flex-1 overflow-y-auto px-3 pb-3 space-y-2">
					<details class="collapse collapse-arrow" open>
						<summary class="collapse-title text-sm font-medium min-h-0 py-2 px-2">📌 Pinned</summary>
						<div class="collapse-content px-1">
							{#if noteStore.pinnedNotes.length === 0}
								<p class="text-xs opacity-40">No pinned notes</p>
							{:else}
								{#each noteStore.pinnedNotes.slice(0, 8) as note}
									<a href="/note/{note.id}" class="block text-sm truncate py-1 hover:text-primary transition-colors">
										{note.title || note.id}
									</a>
								{/each}
							{/if}
						</div>
					</details>

					<details class="collapse collapse-arrow" open>
						<summary class="collapse-title text-sm font-medium min-h-0 py-2 px-2">🕐 Recent</summary>
						<div class="collapse-content px-1">
							{#if recentNotes.length === 0}
								<p class="text-xs opacity-40">No notes yet</p>
							{:else}
								{#each recentNotes as note}
									<a href="/note/{note.id}" class="block text-sm truncate py-1 hover:text-primary transition-colors">
										{note.title || note.id}
									</a>
								{/each}
							{/if}
						</div>
					</details>

					<details class="collapse collapse-arrow">
						<summary class="collapse-title text-sm font-medium min-h-0 py-2 px-2">🏷️ Tags</summary>
						<div class="collapse-content px-1">
							{#if noteStore.allTags.length === 0}
								<p class="text-xs opacity-40">No tags yet</p>
							{:else}
								<div class="flex flex-wrap gap-1">
									{#each noteStore.allTags as tag}
										<button
											class="badge badge-outline badge-sm hover:badge-primary transition-colors"
											class:badge-primary={tagFilter === tag}
											onclick={() => (tagFilter = tag)}
										>
											#{tag}
										</button>
									{/each}
								</div>
							{/if}
						</div>
					</details>

					<a href="/graph" class="btn btn-ghost btn-sm w-full justify-start">🔗 Graph</a>
					<a href="/trash" class="btn btn-ghost btn-sm w-full justify-start">🗑️ Trash</a>
				</nav>

				<div class="p-3 border-t border-base-300/50">
					<button class="btn btn-ghost btn-sm w-full" onclick={() => themeStore.toggle()}>
						{themeStore.current === 'dark' ? 'Light Mode' : 'Dark Mode'}
					</button>
				</div>
			</aside>
		{/if}

		<!-- Main content -->
		<div class="flex-1 flex flex-col overflow-hidden">
			<main class="flex-1 overflow-auto p-4">
				{#if noteStore.ready}
					{@render children()}
				{:else}
					<div class="flex items-center justify-center h-full">
						<span class="loading loading-spinner loading-md opacity-30"></span>
					</div>
				{/if}
			</main>
		</div>
	</div>
</div>
