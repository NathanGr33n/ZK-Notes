<script lang="ts">
	import '../app.css';
	import { themeStore } from '$lib/themeStore.svelte';
	import { noteStore } from '$lib/noteStore.svelte';
	import { slide } from 'svelte/transition';

	let { children } = $props();

	let sidebarOpen = $state(true);
	let searchQuery = $state('');

	const searchResults = $derived.by(() => {
		if (!searchQuery.trim()) return [];
		return noteStore.search(searchQuery).slice(0, 10);
	});
</script>

<div class="flex h-screen overflow-hidden bg-base-100 text-base-content">
	<!-- Sidebar -->
	{#if sidebarOpen}
		<aside
			transition:slide={{ axis: 'x', duration: 200 }}
			class="w-64 flex-shrink-0 border-r border-base-300 bg-base-200 flex flex-col"
		>
			<!-- Brand -->
			<div class="p-4 pb-2">
				<h1 class="text-xl font-bold text-primary">ZK-Notes</h1>
				<p class="text-xs opacity-60">Zettelkasten Knowledge System</p>
			</div>

			<!-- Search -->
			<div class="px-3 py-2 relative">
				<input
					type="text"
					placeholder="Search notes... (Ctrl+K)"
					class="input input-sm input-bordered w-full"
					bind:value={searchQuery}
				/>
				{#if searchResults.length > 0}
					<div class="absolute left-3 right-3 top-full z-50 bg-base-100 border border-base-300 rounded-lg shadow-lg mt-1 max-h-60 overflow-y-auto">
						{#each searchResults as result}
							<a
								href="/note/{result.id}"
								class="block px-3 py-2 text-sm hover:bg-base-200 transition-colors"
								onclick={() => (searchQuery = '')}
							>
								<div class="font-medium truncate">{result.title || result.id}</div>
								<div class="text-xs opacity-50 truncate">{result.content.slice(0, 60)}</div>
							</a>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Create actions -->
			<div class="px-3 py-2 space-y-1">
				<button class="btn btn-primary btn-sm w-full" onclick={() => noteStore.add('permanent')}>
					+ Permanent Note
				</button>
				<div class="flex gap-1">
					<button class="btn btn-ghost btn-xs flex-1" onclick={() => noteStore.add('fleeting')}>
						⚡ Fleeting
					</button>
					<button class="btn btn-ghost btn-xs flex-1" onclick={() => noteStore.add('literature')}>
						📖 Literature
					</button>
				</div>
			</div>

			<div class="divider my-0 mx-3"></div>

			<!-- Navigation -->
			<nav class="flex-1 overflow-y-auto px-3 py-2 space-y-1">
				<a href="/" class="btn btn-ghost btn-sm w-full justify-start">📋 All Notes</a>

				<!-- Tags section (progressive disclosure) -->
				<details class="collapse collapse-arrow">
					<summary class="collapse-title text-sm font-medium min-h-0 py-1 px-2">
						🏷️ Tags
					</summary>
					<div class="collapse-content px-1">
						{#each noteStore.allTags as tag}
							<span class="badge badge-outline badge-sm mr-1 mb-1">#{tag}</span>
						{/each}
						{#if noteStore.allTags.length === 0}
							<p class="text-xs opacity-50">No tags yet</p>
						{/if}
					</div>
				</details>

				<!-- Recent notes -->
				<details class="collapse collapse-arrow" open>
					<summary class="collapse-title text-sm font-medium min-h-0 py-1 px-2">
						🕐 Recent
					</summary>
					<div class="collapse-content px-1 space-y-0.5">
						{#each noteStore.notes.slice().sort((a, b) => b.lastEdit.localeCompare(a.lastEdit)).slice(0, 8) as note}
							<a
								href="/note/{note.id}"
								class="block text-xs truncate py-0.5 hover:text-primary transition-colors"
							>
								{note.title || note.id}
							</a>
						{/each}
					</div>
				</details>
			</nav>

			<!-- Footer -->
			<div class="p-3 border-t border-base-300">
				<button class="btn btn-ghost btn-sm w-full" onclick={() => themeStore.toggle()}>
					{themeStore.current === 'dark' ? '☀️ Light Mode' : '🌙 Dark Mode'}
				</button>
			</div>
		</aside>
	{/if}

	<!-- Main content -->
	<div class="flex-1 flex flex-col overflow-hidden">
		<!-- Top bar -->
		<header class="h-12 flex items-center px-4 border-b border-base-300 bg-base-100 flex-shrink-0">
			<button
				class="btn btn-ghost btn-sm btn-square mr-2"
				onclick={() => (sidebarOpen = !sidebarOpen)}
				aria-label="Toggle sidebar"
			>
				☰
			</button>
			<div class="text-sm font-medium opacity-70">Workspace</div>
		</header>

		<!-- Page content -->
		<main class="flex-1 overflow-auto p-4">
			{@render children()}
		</main>
	</div>
</div>
