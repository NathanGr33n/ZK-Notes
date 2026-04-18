<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { noteStore } from '$lib/noteStore.svelte';
	import { marked } from 'marked';
	import DOMPurify from 'dompurify';
	import { replaceLinksWithHtml } from '$lib/linkParser';
	import type { NoteType } from '$lib/types';
	import LinkAutocomplete from '$lib/components/LinkAutocomplete.svelte';
	import LinkPreview from '$lib/components/LinkPreview.svelte';
	import EditorToolbar from '$lib/components/EditorToolbar.svelte';
	import SlashMenu from '$lib/components/SlashMenu.svelte';

	const noteId = $derived($page.params.id ?? '');
	const note = $derived(noteStore.getById(noteId));
	const backlinks = $derived(noteStore.getBacklinks(noteId));

	let showPreview = $state(false);
	let splitView = $state(false);
	let saveTimer: ReturnType<typeof setTimeout> | null = null;
	let saving = $state(false);
	let editorEl: HTMLTextAreaElement | null = $state(null);
	let autocomplete: LinkAutocomplete | null = $state(null);
	let slashMenu: SlashMenu | null = $state(null);
	let toolbar: EditorToolbar | null = $state(null);
	let previewEl: HTMLDivElement | null = $state(null);
	let linkPreview: LinkPreview | null = $state(null);

	const wordCount = $derived.by(() => {
		if (!note) return 0;
		const words = note.content.trim().split(/\s+/).filter(Boolean);
		return words.length;
	});
	const readingTime = $derived(Math.max(1, Math.ceil(wordCount / 200)));

	function autoSave(field: string, value: string) {
		saving = true;
		if (saveTimer) clearTimeout(saveTimer);
		saveTimer = setTimeout(() => {
			noteStore.update(noteId, { [field]: value });
			saving = false;
		}, 320);
	}

	function handleContentChange(newContent: string) {
		noteStore.update(noteId, { content: newContent });
	}

	function archiveNote() {
		if (confirm(`Move ${noteId} to trash?`)) {
			noteStore.archive(noteId);
			goto('/');
		}
	}

	function deleteNote() {
		if (confirm(`Permanently delete note ${noteId}?`)) {
			noteStore.remove(noteId);
			goto('/');
		}
	}

	function promoteNote() {
		if (note && (note.type === 'fleeting' || note.type === 'literature')) {
			noteStore.update(noteId, { type: 'permanent' });
		}
	}

	function renderContent(content: string): string {
		const withLinks = replaceLinksWithHtml(content);
		const raw = marked.parse(withLinks, { async: false }) as string;
		return DOMPurify.sanitize(raw, {
			ADD_ATTR: ['data-note-link'],
		});
	}

	function handlePreviewClick(e: MouseEvent) {
		const target = e.target as HTMLElement;
		const link = target.closest('[data-note-link]') as HTMLElement | null;
		if (!link) return;
		e.preventDefault();
		const noteRef = decodeURIComponent(link.dataset.noteLink ?? '');
		if (!noteRef) return;
		const found = noteStore.getById(noteRef)
			?? noteStore.notes.find((n) => n.title.toLowerCase() === noteRef.toLowerCase());
		if (found) goto(`/note/${found.id}`);
	}

	function handleEditorKeydown(e: KeyboardEvent) {
		if (autocomplete?.handleKeydown(e)) return;
		if (slashMenu?.handleKeydown(e)) return;
		toolbar?.handleKeydown(e);
	}

	function handleEditorInput(e: Event) {
		const value = (e.target as HTMLTextAreaElement).value;
		autoSave('content', value);
		autocomplete?.handleInput();
		slashMenu?.handleInput();
	}

	$effect(() => {
		if (previewEl && linkPreview) {
			const cleanup = linkPreview.attach(previewEl);
			return cleanup;
		}
	});
</script>

<LinkPreview bind:this={linkPreview} />

{#if note}
	<div class="max-w-5xl mx-auto space-y-4">
		<div class="flex items-center gap-2 text-xs opacity-65 flex-wrap">
			<a href="/" class="btn btn-ghost btn-xs">&larr; Notes</a>
			<span>/</span>
			<span class="font-mono">{note.id}</span>
			<div class="ml-auto flex items-center gap-2 flex-wrap">
				<button
					class="btn btn-ghost btn-xs"
					class:btn-active={note.pinned}
					onclick={() => noteStore.togglePin(noteId)}
				>
					{note.pinned ? 'Pinned' : 'Pin'}
				</button>
				<select
					class="select select-xs select-bordered"
					value={note.type}
					onchange={(e) => noteStore.update(noteId, { type: e.currentTarget.value as NoteType })}
				>
					<option value="permanent">Permanent</option>
					<option value="literature">Literature</option>
					<option value="fleeting">Fleeting</option>
					<option value="standard">Standard</option>
				</select>
				{#if note.type === 'fleeting' || note.type === 'literature'}
					<button class="btn btn-xs btn-outline" onclick={promoteNote}>
						Promote
					</button>
				{/if}
				<button class="btn btn-ghost btn-xs text-error" onclick={archiveNote}>Move to trash</button>
				{#if saving}
					<span class="text-xs opacity-50">Saving…</span>
				{/if}
			</div>
		</div>

		<input
			type="text"
			class="w-full bg-transparent border-none text-4xl font-semibold px-0 focus:outline-none placeholder:opacity-30 tracking-tight"
			placeholder="Untitled"
			value={note.title}
			oninput={(e) => autoSave('title', e.currentTarget.value)}
		/>

		{#if note.tags.length > 0}
			<div class="flex flex-wrap gap-1.5">
				{#each note.tags as tag}
					<span class="badge badge-outline badge-sm">#{tag}</span>
				{/each}
			</div>
		{/if}

		<div class="surface p-1 inline-flex gap-1">
			<button
				class="btn btn-ghost btn-sm"
				class:btn-active={!showPreview && !splitView}
				onclick={() => { showPreview = false; splitView = false; }}
			>
				Edit
			</button>
			<button
				class="btn btn-ghost btn-sm"
				class:btn-active={splitView}
				onclick={() => { splitView = true; showPreview = false; }}
			>
				Split
			</button>
			<button
				class="btn btn-ghost btn-sm"
				class:btn-active={showPreview}
				onclick={() => { showPreview = true; splitView = false; }}
			>
				Preview
			</button>
		</div>

		{#if showPreview}
			<!-- svelte-ignore a11y_click_events_have_key_events -->
			<!-- svelte-ignore a11y_no_static_element_interactions -->
			<div
				bind:this={previewEl}
				class="prose max-w-none min-h-[520px] px-10 py-8 bg-base-100 border border-base-300/80 rounded-md"
				onclick={handlePreviewClick}
			>
				{@html renderContent(note.content)}
			</div>
		{:else if splitView}
			<div class="grid grid-cols-2 gap-4 min-h-[520px]">
				<div class="space-y-2">
					<EditorToolbar bind:this={toolbar} textarea={editorEl} onContentChange={handleContentChange} />
					<div class="relative">
						<textarea
							bind:this={editorEl}
							class="textarea textarea-bordered w-full min-h-[470px] font-mono text-sm leading-relaxed resize-none"
							placeholder="Write in Markdown... Use [[Note Title]] to link. Type / for commands."
							value={note.content}
							oninput={handleEditorInput}
							onkeydown={handleEditorKeydown}
						></textarea>
						<LinkAutocomplete bind:this={autocomplete} textarea={editorEl} onInsert={handleContentChange} />
						<SlashMenu bind:this={slashMenu} textarea={editorEl} onInsert={handleContentChange} />
					</div>
				</div>
				<!-- svelte-ignore a11y_click_events_have_key_events -->
				<!-- svelte-ignore a11y_no_static_element_interactions -->
				<div
					bind:this={previewEl}
					class="prose max-w-none min-h-[520px] px-8 py-6 bg-base-100 border border-base-300/80 rounded-md overflow-auto"
					onclick={handlePreviewClick}
				>
					{@html renderContent(note.content)}
				</div>
			</div>
		{:else}
			<div class="space-y-2">
				<EditorToolbar bind:this={toolbar} textarea={editorEl} onContentChange={handleContentChange} />
				<div class="relative">
					<textarea
						bind:this={editorEl}
						class="textarea textarea-bordered w-full min-h-[520px] font-mono text-sm leading-relaxed resize-y"
						placeholder="Write in Markdown... Use [[Note Title]] to link. Type / for commands."
						value={note.content}
						oninput={handleEditorInput}
						onkeydown={handleEditorKeydown}
					></textarea>
					<LinkAutocomplete bind:this={autocomplete} textarea={editorEl} onInsert={handleContentChange} />
					<SlashMenu bind:this={slashMenu} textarea={editorEl} onInsert={handleContentChange} />
				</div>
			</div>
		{/if}

		<div class="flex items-center gap-4 text-xs opacity-55 flex-wrap">
			<span>{wordCount} words</span>
			<span>~{readingTime} min read</span>
			<span>Created {new Date(note.created).toLocaleDateString()}</span>
			<span>Edited {new Date(note.lastEdit).toLocaleString()}</span>
			<div class="flex-1"></div>
			<button class="btn btn-ghost btn-xs text-error" onclick={deleteNote}>Delete permanently</button>
		</div>

		{#if backlinks.length > 0}
			<div class="pt-2">
				<h4 class="text-sm font-medium opacity-65 mb-2">Backlinks ({backlinks.length})</h4>
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
					{#each backlinks as blId}
						{@const blNote = noteStore.getById(blId)}
						{#if blNote}
							<a href="/note/{blId}" class="surface p-3 notion-surface-hover">
								<div class="text-sm font-medium">{blNote.title || blId}</div>
								<div class="text-xs opacity-55 truncate mt-1">{blNote.content.slice(0, 90)}</div>
							</a>
						{/if}
					{/each}
				</div>
			</div>
		{/if}
	</div>
{:else}
	<div class="surface p-8 text-center max-w-xl mx-auto">
		<p class="text-lg font-medium mb-2">Page not found</p>
		<a href="/" class="btn btn-sm">Back to notes</a>
	</div>
{/if}
