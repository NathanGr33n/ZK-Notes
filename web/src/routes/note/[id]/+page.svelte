<script lang="ts">
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { noteStore } from '$lib/noteStore.svelte';
	import { marked } from 'marked';
	import DOMPurify from 'dompurify';
	import { replaceLinksWithHtml } from '$lib/linkParser';
	import type { NoteType, NoteColor } from '$lib/types';
	import { NOTE_COLORS } from '$lib/types';
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
	let showColorPicker = $state(false);

	/** Word count and reading time. */
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
		}, 400);
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
		if (note && note.type === 'fleeting') {
			noteStore.update(noteId, { type: 'permanent' });
		} else if (note && note.type === 'literature') {
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
		<!-- Color accent strip -->
		{#if note.color && note.color !== 'none'}
			<div class="note-accent-{note.color} rounded-t-lg h-1"></div>
		{/if}

		<!-- Top bar -->
		<div class="flex items-center gap-2 flex-wrap">
			<a href="/" class="btn btn-ghost btn-sm">&larr; Back</a>
			<span class="font-mono text-xs opacity-30">{note.id}</span>

			<div class="flex-1"></div>

			<!-- Pin -->
			<button
				class="btn btn-ghost btn-sm btn-square"
				class:btn-active={note.pinned}
				onclick={() => noteStore.togglePin(noteId)}
				title={note.pinned ? 'Unpin' : 'Pin'}
			>
				📌
			</button>

			<!-- Color picker -->
			<div class="relative">
				<button
					class="btn btn-ghost btn-sm btn-square"
					onclick={() => (showColorPicker = !showColorPicker)}
					title="Note color"
				>
					🎨
				</button>
				{#if showColorPicker}
					<div class="absolute right-0 top-full mt-1 z-50 surface p-2 flex gap-1 animate-scale-in">
						{#each NOTE_COLORS as color}
							<button
								class="w-5 h-5 rounded-full border-2 note-accent-{color}"
								class:border-primary={note.color === color}
								class:border-transparent={note.color !== color}
								style="background: {color === 'none' ? 'rgba(255,255,255,0.1)' : ''};"
								onclick={() => { noteStore.setColor(noteId, color); showColorPicker = false; }}
								title={color}
							></button>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Note type -->
			<select
				class="select select-sm select-bordered text-xs"
				value={note.type}
				onchange={(e) => noteStore.update(noteId, { type: e.currentTarget.value as NoteType })}
			>
				<option value="permanent">Permanent</option>
				<option value="literature">Literature</option>
				<option value="fleeting">Fleeting</option>
				<option value="standard">Standard</option>
			</select>

			<!-- Promote -->
			{#if note.type === 'fleeting' || note.type === 'literature'}
				<button class="btn btn-sm btn-outline" onclick={promoteNote}>
					⬆ Promote to Permanent
				</button>
			{/if}

			<!-- View toggle -->
			<div class="flex gap-1">
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

			<!-- Trash -->
			<button class="btn btn-sm btn-ghost" onclick={archiveNote} title="Move to trash">🗑️</button>

			{#if saving}
				<span class="text-xs opacity-40">Saving...</span>
			{/if}
		</div>

		<!-- Title -->
		<input
			type="text"
			class="w-full bg-transparent border-none text-xl font-bold px-0 focus:outline-none placeholder:opacity-25"
			placeholder="Note title…"
			value={note.title}
			oninput={(e) => autoSave('title', e.currentTarget.value)}
		/>

		<!-- Tags -->
		{#if note.tags.length > 0}
			<div class="flex flex-wrap items-center gap-1">
				{#each note.tags as tag}
					<span class="badge badge-outline badge-sm">#{tag}</span>
				{/each}
			</div>
		{/if}

		<!-- Editor / Preview / Split -->
		{#if showPreview}
			<!-- svelte-ignore a11y_click_events_have_key_events -->
			<!-- svelte-ignore a11y_no_static_element_interactions -->
			<div
				bind:this={previewEl}
				class="prose prose-sm max-w-none min-h-[400px] p-6 surface"
				onclick={handlePreviewClick}
			>
				{@html renderContent(note.content)}
			</div>
		{:else if splitView}
			<EditorToolbar bind:this={toolbar} textarea={editorEl} onContentChange={handleContentChange} />
			<div class="grid grid-cols-2 gap-4 min-h-[400px]">
				<div class="relative">
					<textarea
						bind:this={editorEl}
						class="textarea textarea-bordered w-full h-full font-mono text-sm leading-relaxed resize-none"
						placeholder="Write in Markdown... Use [[Note Title]] to link. Type / for commands."
						value={note.content}
						oninput={handleEditorInput}
						onkeydown={handleEditorKeydown}
					></textarea>
					<LinkAutocomplete bind:this={autocomplete} textarea={editorEl} onInsert={handleContentChange} />
					<SlashMenu bind:this={slashMenu} textarea={editorEl} onInsert={handleContentChange} />
				</div>
				<!-- svelte-ignore a11y_click_events_have_key_events -->
				<!-- svelte-ignore a11y_no_static_element_interactions -->
				<div
					bind:this={previewEl}
					class="prose prose-sm max-w-none p-4 surface overflow-auto"
					onclick={handlePreviewClick}
				>
					{@html renderContent(note.content)}
				</div>
			</div>
		{:else}
			<EditorToolbar bind:this={toolbar} textarea={editorEl} onContentChange={handleContentChange} />
			<div class="relative">
				<textarea
					bind:this={editorEl}
					class="textarea textarea-bordered w-full min-h-[400px] font-mono text-sm leading-relaxed"
					placeholder="Write in Markdown... Use [[Note Title]] to link. Type / for commands."
					value={note.content}
					oninput={handleEditorInput}
					onkeydown={handleEditorKeydown}
				></textarea>
				<LinkAutocomplete bind:this={autocomplete} textarea={editorEl} onInsert={handleContentChange} />
				<SlashMenu bind:this={slashMenu} textarea={editorEl} onInsert={handleContentChange} />
			</div>
		{/if}

		<!-- Metadata footer -->
		<div class="flex items-center gap-4 text-xs opacity-30 flex-wrap">
			<span>{wordCount} words</span>
			<span>~{readingTime} min read</span>
			<span>·</span>
			<span>Created {new Date(note.created).toLocaleDateString()}</span>
			<span>Edited {new Date(note.lastEdit).toLocaleString()}</span>
			<div class="flex-1"></div>
			<button class="btn btn-ghost btn-xs text-error" onclick={deleteNote}>Delete permanently</button>
		</div>

		<!-- Backlinks -->
		{#if backlinks.length > 0}
			<div class="border-t border-base-300/50 pt-4">
				<h4 class="text-sm font-medium opacity-50 mb-3">
					Backlinks ({backlinks.length})
				</h4>
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
					{#each backlinks as blId}
						{@const blNote = noteStore.getById(blId)}
						{#if blNote}
							<a
								href="/note/{blId}"
								class="surface p-3 hover:border-primary/30 transition-colors"
							>
								<div class="text-sm font-medium">{blNote.title || blId}</div>
								<div class="text-xs opacity-40 truncate mt-1">{blNote.content.slice(0, 80)}</div>
							</a>
						{/if}
					{/each}
				</div>
			</div>
		{/if}
	</div>
{:else}
	<div class="flex flex-col items-center justify-center py-24">
		<div class="surface p-8 text-center">
			<p class="text-lg mb-2 opacity-60">Note not found</p>
			<a href="/" class="btn btn-sm btn-primary">Back to workspace</a>
		</div>
	</div>
{/if}
