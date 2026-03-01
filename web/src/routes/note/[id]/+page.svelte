<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { noteStore } from '$lib/noteStore.svelte';
	import { marked } from 'marked';
	import { replaceLinksWithHtml } from '$lib/linkParser';
	import type { NoteType } from '$lib/types';
	import LinkAutocomplete from '$lib/components/LinkAutocomplete.svelte';
	import LinkPreview from '$lib/components/LinkPreview.svelte';
	import { onMount } from 'svelte';

	const noteId = $derived($page.params.id ?? '');
	const note = $derived(noteStore.getById(noteId));
	const backlinks = $derived(noteStore.getBacklinks(noteId));

	let showPreview = $state(false);
	let saveTimer: ReturnType<typeof setTimeout> | null = null;
	let saving = $state(false);
	let editorEl: HTMLTextAreaElement | null = $state(null);
	let autocomplete: LinkAutocomplete | null = $state(null);
	let previewEl: HTMLDivElement | null = $state(null);
	let linkPreview: LinkPreview | null = $state(null);

	/** Debounced auto-save on content/title change. */
	function autoSave(field: string, value: string) {
		saving = true;
		if (saveTimer) clearTimeout(saveTimer);
		saveTimer = setTimeout(() => {
			noteStore.update(noteId, { [field]: value });
			saving = false;
		}, 400);
	}

	function deleteNote() {
		if (confirm(`Delete note ${noteId}?`)) {
			noteStore.remove(noteId);
			goto('/');
		}
	}

	/** Render markdown content with wiki-links resolved. */
	function renderContent(content: string): string {
		const withLinks = replaceLinksWithHtml(content);
		return marked.parse(withLinks, { async: false }) as string;
	}

	/** Handle click on internal note links in preview. */
	function handlePreviewClick(e: MouseEvent) {
		const target = e.target as HTMLElement;
		const link = target.closest('[data-note-link]') as HTMLElement | null;
		if (!link) return;

		e.preventDefault();
		const noteRef = decodeURIComponent(link.dataset.noteLink ?? '');
		if (!noteRef) return;

		// Try to find by ID first, then by title
		const found = noteStore.getById(noteRef)
			?? noteStore.notes.find((n) => n.title.toLowerCase() === noteRef.toLowerCase());

		if (found) {
			goto(`/note/${found.id}`);
		}
	}

	/** Handle autocomplete insertion. */
	function handleAutocompleteInsert(newContent: string) {
		noteStore.update(noteId, { content: newContent });
	}

	// Attach link preview hover listeners when preview pane is shown
	$effect(() => {
		if (previewEl && linkPreview) {
			const cleanup = linkPreview.attach(previewEl);
			return cleanup;
		}
	});
</script>

<LinkPreview bind:this={linkPreview} />

{#if note}
	<div class="max-w-4xl mx-auto space-y-4">
		<!-- Top bar -->
		<div class="flex items-center gap-2 flex-wrap">
			<a href="/" class="btn btn-ghost btn-sm">&larr; Back</a>
			<span class="font-mono text-xs opacity-40">{note.id}</span>

			<div class="flex-1"></div>

			<select
				class="select select-sm select-bordered"
				value={note.type}
				onchange={(e) => noteStore.update(noteId, { type: e.currentTarget.value as NoteType })}
			>
				<option value="permanent">Permanent</option>
				<option value="literature">Literature</option>
				<option value="fleeting">Fleeting</option>
				<option value="standard">Standard</option>
			</select>

			<button
				class="btn btn-sm"
				class:btn-active={showPreview}
				onclick={() => (showPreview = !showPreview)}
			>
				{showPreview ? 'Edit' : 'Preview'}
			</button>

			<button class="btn btn-sm btn-error btn-outline" onclick={deleteNote}>Delete</button>

			{#if saving}
				<span class="text-xs opacity-50">Saving...</span>
			{/if}
		</div>

		<!-- Title -->
		<input
			type="text"
			class="input input-lg input-ghost w-full text-xl font-bold px-0 focus:outline-none"
			placeholder="Note title…"
			value={note.title}
			oninput={(e) => autoSave('title', e.currentTarget.value)}
		/>

		<!-- Tags input -->
		<div class="flex flex-wrap items-center gap-1">
			{#each note.tags as tag}
				<span class="badge badge-outline badge-sm">#{tag}</span>
			{/each}
		</div>

		<!-- Editor / Preview -->
		{#if showPreview}
			<!-- svelte-ignore a11y_click_events_have_key_events -->
			<!-- svelte-ignore a11y_no_static_element_interactions -->
			<div
				bind:this={previewEl}
				class="prose prose-sm max-w-none min-h-[300px] p-4 bg-base-200 rounded-lg"
				onclick={handlePreviewClick}
			>
				{@html renderContent(note.content)}
			</div>
		{:else}
			<div class="relative">
				<textarea
					bind:this={editorEl}
					class="textarea textarea-bordered w-full min-h-[400px] font-mono text-sm leading-relaxed"
					placeholder="Write your note in Markdown... Use [[Note Title]] to link to other notes."
					value={note.content}
					oninput={(e) => {
						autoSave('content', e.currentTarget.value);
						autocomplete?.handleInput();
					}}
					onkeydown={(e) => autocomplete?.handleKeydown(e)}
				></textarea>
				<LinkAutocomplete
					bind:this={autocomplete}
					textarea={editorEl}
					onInsert={handleAutocompleteInsert}
				/>
			</div>
		{/if}

		<!-- Metadata -->
		<div class="text-xs opacity-40 flex gap-4">
			<span>Created: {new Date(note.created).toLocaleDateString()}</span>
			<span>Edited: {new Date(note.lastEdit).toLocaleString()}</span>
		</div>

		<!-- Backlinks panel -->
		{#if backlinks.length > 0}
			<div class="border-t border-base-300 pt-4">
				<h4 class="text-sm font-semibold opacity-60 mb-2">
					Backlinks ({backlinks.length})
				</h4>
				<div class="space-y-1">
					{#each backlinks as blId}
						{@const blNote = noteStore.getById(blId)}
						{#if blNote}
							<a
								href="/note/{blId}"
								class="block text-sm hover:text-primary transition-colors"
							>
								{blNote.title || blId}
							</a>
						{/if}
					{/each}
				</div>
			</div>
		{/if}
	</div>
{:else}
	<div class="flex flex-col items-center justify-center py-20 opacity-50">
		<p class="text-lg mb-2">Note not found</p>
		<a href="/" class="btn btn-sm btn-primary">Back to workspace</a>
	</div>
{/if}
