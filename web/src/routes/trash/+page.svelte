<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';

	function restoreNote(id: string) {
		noteStore.restore(id);
	}

	function permanentDelete(id: string) {
		if (confirm(`Permanently delete ${id}? This cannot be undone.`)) {
			noteStore.remove(id);
		}
	}

	function emptyTrash() {
		if (confirm(`Permanently delete all ${noteStore.archivedNotes.length} notes in trash?`)) {
			for (const note of noteStore.archivedNotes) {
				noteStore.remove(note.id);
			}
		}
	}
</script>

<div class="max-w-3xl mx-auto space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h2 class="text-2xl font-semibold tracking-tight">Trash</h2>
			<p class="text-xs opacity-55 mt-1">{noteStore.archivedNotes.length} page{noteStore.archivedNotes.length === 1 ? '' : 's'} in trash</p>
		</div>
		<div class="flex gap-2">
			<a href="/" class="btn btn-ghost btn-sm">&larr; Back</a>
			{#if noteStore.archivedNotes.length > 0}
				<button class="btn btn-error btn-sm btn-outline" onclick={emptyTrash}>Empty Trash</button>
			{/if}
		</div>
	</div>

	{#if noteStore.archivedNotes.length === 0}
		<div class="flex flex-col items-center justify-center py-20">
			<div class="surface p-8 text-center min-w-80">
				<p class="text-sm opacity-60">Trash is empty</p>
			</div>
		</div>
	{:else}
		<div class="space-y-2">
			{#each noteStore.archivedNotes as note (note.id)}
				<div class="surface p-4 flex items-center gap-4">
					<div class="flex-1 min-w-0">
						<div class="text-sm font-medium truncate">{note.title || 'Untitled'}</div>
						<div class="text-xs opacity-55 mt-1 flex gap-3">
							<span class="font-mono">{note.id}</span>
							<span class="capitalize">{note.type}</span>
							<span>Edited {new Date(note.lastEdit).toLocaleDateString()}</span>
						</div>
					</div>
					<button class="btn btn-ghost btn-sm" onclick={() => restoreNote(note.id)}>
						Restore
					</button>
					<button class="btn btn-ghost btn-sm text-error" onclick={() => permanentDelete(note.id)}>
						Delete
					</button>
				</div>
			{/each}
		</div>
	{/if}
</div>
