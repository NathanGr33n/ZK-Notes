<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { THEMES, themeStore } from '$lib/themeStore.svelte';
	import { noteStore } from '$lib/noteStore.svelte';
	import { slide } from 'svelte/transition';
	import { onMount } from 'svelte';
	import TitleBar from '$lib/components/TitleBar.svelte';
	import CommandPalette from '$lib/components/CommandPalette.svelte';
	import QuickCapture from '$lib/components/QuickCapture.svelte';
	type AppTheme = (typeof THEMES)[number];

	let { children } = $props();
	let storageDiagnostics: Awaited<ReturnType<typeof noteStore.getStorageDiagnostics>> | null = $state(null);
	let importFeedback = $state('');
	let importError = $state('');
	let importInputEl: HTMLInputElement | null = $state(null);

	onMount(() => {
		noteStore.init().then(() => {
			void refreshStorageDiagnostics();
		});
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

	async function refreshStorageDiagnostics() {
		try {
			storageDiagnostics = await noteStore.getStorageDiagnostics();
		} catch {
			storageDiagnostics = null;
		}
	}

	function triggerImport() {
		importInputEl?.click();
	}

	function exportBackup() {
		const payload = noteStore.exportNotes();
		const blob = new Blob([payload], { type: 'application/json;charset=utf-8' });
		const url = URL.createObjectURL(blob);
		const anchor = document.createElement('a');
		anchor.href = url;
		anchor.download = `zk-notes-backup-${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.json`;
		anchor.click();
		URL.revokeObjectURL(url);
		importFeedback = 'Backup exported locally.';
		importError = '';
		void refreshStorageDiagnostics();
	}

	async function importBackup(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		if (!file) return;

		try {
			const text = await file.text();
			const result = noteStore.importNotes(text);
			if (result.added === 0) {
				importFeedback = '';
				importError = 'No new notes were imported. Backup may be invalid or already merged.';
			} else {
				const skippedSuffix = result.skipped > 0 ? ` (${result.skipped} skipped)` : '';
				importFeedback = `Imported ${result.added} note${result.added === 1 ? '' : 's'}${skippedSuffix}.`;
				importError = '';
			}
			await refreshStorageDiagnostics();
		} catch {
			importFeedback = '';
			importError = 'Import failed. Please use a valid local backup JSON file.';
		} finally {
			input.value = '';
		}
	}

	function formatTimestamp(iso: string | null): string {
		if (!iso) return 'Never';
		const date = new Date(iso);
		return `${date.toLocaleDateString()} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
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

				<div class="mt-auto p-3 border-t border-base-300/70 space-y-2.5">
					<div class="space-y-1">
						<div class="text-[10px] uppercase tracking-wide opacity-50 font-semibold">Appearance</div>
						<select
							class="select select-xs w-full"
							value={themeStore.current}
							onchange={(e) => themeStore.set(e.currentTarget.value as AppTheme)}
						>
							{#each THEMES as theme}
								<option value={theme}>{theme[0].toUpperCase() + theme.slice(1)}</option>
							{/each}
						</select>
						<div class="flex gap-1.5">
							<button class="btn btn-ghost btn-xs flex-1" onclick={() => themeStore.toggleDensity()}>
								{themeStore.density === 'compact' ? 'Compact' : 'Comfortable'}
							</button>
							<button
								class="btn btn-ghost btn-xs flex-1"
								onclick={() => themeStore.setMotionMode(themeStore.motion === 'full' ? 'reduced' : 'full')}
							>
								{themeStore.motion === 'full' ? 'Motion: Full' : 'Motion: Reduced'}
							</button>
						</div>
					</div>

					<div class="space-y-1.5">
						<div class="text-[10px] uppercase tracking-wide opacity-50 font-semibold">Storage</div>
						<div class="text-[11px] opacity-65 leading-relaxed">
							Local-only ({storageDiagnostics?.provider ?? 'indexeddb'}) · {storageDiagnostics?.noteCount ?? noteStore.notes.length} notes
						</div>
						<div class="text-[10px] opacity-50 leading-relaxed">
							Last backup: {formatTimestamp(storageDiagnostics?.lastBackupAt ?? null)}
						</div>
						<div class="text-[10px] opacity-50 leading-relaxed">
							Last import: {formatTimestamp(storageDiagnostics?.lastImportAt ?? null)}
						</div>
						<div class="flex gap-1.5">
							<button class="btn btn-outline btn-xs flex-1" onclick={exportBackup}>Export backup</button>
							<button class="btn btn-outline btn-xs flex-1" onclick={triggerImport}>Import backup</button>
						</div>
						<input
							bind:this={importInputEl}
							type="file"
							accept=".json,application/json"
							class="hidden"
							onchange={importBackup}
						/>
						{#if importFeedback}
							<p class="text-[10px] text-success leading-relaxed">{importFeedback}</p>
						{/if}
						{#if importError}
							<p class="text-[10px] text-error leading-relaxed">{importError}</p>
						{/if}
					</div>
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
