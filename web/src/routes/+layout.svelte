<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
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
	let commandPalette: CommandPalette | null = $state(null);

	function openPalette() {
		commandPalette?.open();
	}

	function createPage() {
		const note = noteStore.add('standard');
		goto(`/note/${note.id}`);
	}

	const currentPath = $derived($page.url.pathname);
	const pinnedNotes = $derived(noteStore.pinnedNotes.slice(0, 10));
	const recentNotes = $derived.by(() =>
		noteStore.activeNotes
			.slice()
			.sort((a, b) => b.lastEdit.localeCompare(a.lastEdit))
			.slice(0, 30)
	);
</script>

<CommandPalette bind:this={commandPalette} />
<QuickCapture />

<div class="notion-app-shell bg-base-100 text-base-content">
	<TitleBar onToggleSidebar={() => (sidebarOpen = !sidebarOpen)} onOpenSearch={openPalette} />

	<div class="flex flex-1 overflow-hidden">
		{#if sidebarOpen}
			<aside
				transition:slide={{ axis: 'x', duration: 150 }}
				class="w-72 flex-shrink-0 border-r border-base-300/80 bg-base-200/45 flex flex-col overflow-hidden"
			>
				<div class="p-3 border-b border-base-300/70 space-y-2">
					<button class="btn btn-sm w-full justify-start" onclick={createPage}>
						+ New page
					</button>
					<button class="btn btn-ghost btn-sm w-full justify-start" onclick={openPalette}>
						Search notes
						<span class="ml-auto opacity-55 text-[10px]">Ctrl+K</span>
					</button>
				</div>

				<nav class="p-2 space-y-1">
					<a href="/" class="notion-sidebar-link" class:notion-sidebar-link-active={currentPath === '/'}>
						<span>📝</span>
						<span>Notes</span>
					</a>
					<a href="/trash" class="notion-sidebar-link" class:notion-sidebar-link-active={currentPath === '/trash'}>
						<span>🗑️</span>
						<span>Trash</span>
					</a>
				</nav>

				<div class="px-3 pt-2 pb-1 text-[10px] uppercase tracking-wide opacity-45 font-semibold">Favorites</div>
				<div class="px-2 pb-2 space-y-0.5">
					{#if pinnedNotes.length === 0}
						<p class="px-2 py-1 text-xs opacity-45">No pinned notes</p>
					{:else}
						{#each pinnedNotes as note}
							<a
								href="/note/{note.id}"
								class="notion-sidebar-link"
								class:notion-sidebar-link-active={currentPath === `/note/${note.id}`}
							>
								<span class="truncate">{note.title || note.id}</span>
							</a>
						{/each}
					{/if}
				</div>

				<div class="px-3 pt-2 pb-1 text-[10px] uppercase tracking-wide opacity-45 font-semibold">All pages</div>
				<div class="px-2 pb-2 overflow-y-auto space-y-0.5">
					{#if recentNotes.length === 0}
						<p class="px-2 py-1 text-xs opacity-45">No pages yet</p>
					{:else}
						{#each recentNotes as note}
							<a
								href="/note/{note.id}"
								class="notion-sidebar-link"
								class:notion-sidebar-link-active={currentPath === `/note/${note.id}`}
							>
								<span class="truncate">{note.title || note.id}</span>
							</a>
						{/each}
					{/if}
				</div>

				<div class="mt-auto p-3 border-t border-base-300/70">
					<button class="btn btn-ghost btn-sm w-full justify-start" onclick={() => themeStore.toggle()}>
						{themeStore.current === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
					</button>
				</div>
			</aside>
		{/if}

		<main class="flex-1 overflow-auto bg-base-100">
			<div class="h-full max-w-6xl mx-auto px-6 py-5">
				{#if noteStore.ready}
					{@render children()}
				{:else}
					<div class="flex items-center justify-center h-full">
						<span class="loading loading-spinner loading-md opacity-35"></span>
					</div>
				{/if}
			</div>
		</main>
	</div>
</div>
