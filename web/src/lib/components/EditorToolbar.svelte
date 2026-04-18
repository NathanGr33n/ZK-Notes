<script lang="ts">
	interface Props {
		textarea: HTMLTextAreaElement | null;
		onContentChange: (content: string) => void;
	}

	const { textarea, onContentChange }: Props = $props();

	interface ToolbarAction {
		label: string;
		icon: string;
		shortcut?: string;
		action: () => void;
	}

	function wrapSelection(before: string, after: string) {
		if (!textarea) return;
		const start = textarea.selectionStart;
		const end = textarea.selectionEnd;
		const val = textarea.value;
		const selected = val.slice(start, end) || 'text';
		const newVal = val.slice(0, start) + before + selected + after + val.slice(end);
		onContentChange(newVal);
		requestAnimationFrame(() => {
			if (!textarea) return;
			textarea.selectionStart = start + before.length;
			textarea.selectionEnd = start + before.length + selected.length;
			textarea.focus();
		});
	}

	function insertAtLineStart(prefix: string) {
		if (!textarea) return;
		const val = textarea.value;
		const pos = textarea.selectionStart;
		const lineStart = val.lastIndexOf('\n', pos - 1) + 1;
		const newVal = val.slice(0, lineStart) + prefix + val.slice(lineStart);
		onContentChange(newVal);
		requestAnimationFrame(() => {
			if (!textarea) return;
			textarea.selectionStart = pos + prefix.length;
			textarea.selectionEnd = pos + prefix.length;
			textarea.focus();
		});
	}

	function insertBlock(block: string) {
		if (!textarea) return;
		const pos = textarea.selectionStart;
		const val = textarea.value;
		const needsNewline = pos > 0 && val[pos - 1] !== '\n' ? '\n' : '';
		const newVal = val.slice(0, pos) + needsNewline + block + '\n' + val.slice(pos);
		onContentChange(newVal);
		requestAnimationFrame(() => {
			if (!textarea) return;
			const newPos = pos + needsNewline.length + block.length + 1;
			textarea.selectionStart = newPos;
			textarea.selectionEnd = newPos;
			textarea.focus();
		});
	}

	const actions: ToolbarAction[] = [
		{ label: 'Bold', icon: 'B', shortcut: 'Ctrl+B', action: () => wrapSelection('**', '**') },
		{ label: 'Italic', icon: 'I', shortcut: 'Ctrl+I', action: () => wrapSelection('*', '*') },
		{ label: 'Strikethrough', icon: 'S', action: () => wrapSelection('~~', '~~') },
		{ label: 'Heading 1', icon: 'H1', action: () => insertAtLineStart('# ') },
		{ label: 'Heading 2', icon: 'H2', action: () => insertAtLineStart('## ') },
		{ label: 'Heading 3', icon: 'H3', action: () => insertAtLineStart('### ') },
		{ label: 'Bullet List', icon: '•', action: () => insertAtLineStart('- ') },
		{ label: 'Numbered List', icon: '1.', action: () => insertAtLineStart('1. ') },
		{ label: 'Code', icon: '`', action: () => wrapSelection('`', '`') },
		{ label: 'Code Block', icon: '```', action: () => insertBlock('```\n\n```') },
		{ label: 'Link', icon: '🔗', action: () => wrapSelection('[[', ']]') },
		{ label: 'Divider', icon: '—', action: () => insertBlock('---') },
		{ label: 'Quote', icon: '"', action: () => insertAtLineStart('> ') },
	];

	/** Handle keyboard shortcuts. Returns true if handled. */
	export function handleKeydown(e: KeyboardEvent): boolean {
		if (!e.ctrlKey && !e.metaKey) return false;

		if (e.key === 'b') {
			e.preventDefault();
			wrapSelection('**', '**');
			return true;
		}
		if (e.key === 'i') {
			e.preventDefault();
			wrapSelection('*', '*');
			return true;
		}
		if (e.key === 'e') {
			e.preventDefault();
			wrapSelection('`', '`');
			return true;
		}
		return false;
	}
</script>

<div class="editor-toolbar">
	{#each actions as act}
		<button
			class="toolbar-btn"
			title="{act.label}{act.shortcut ? ` (${act.shortcut})` : ''}"
			onclick={act.action}
		>
			{act.icon}
		</button>
	{/each}
</div>

<style>
	.editor-toolbar {
		display: flex;
		align-items: center;
		gap: 2px;
		padding: 6px 8px;
		background: oklch(var(--b2));
		border: 1px solid oklch(var(--b3) / 0.82);
		border-radius: 8px;
		flex-wrap: wrap;
	}

	.toolbar-btn {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		border: none;
		background: transparent;
		color: oklch(var(--bc) / 0.65);
		border-radius: 5px;
		cursor: pointer;
		font-size: 12px;
		font-weight: 600;
		transition: all 0.1s ease;
		font-family: ui-monospace, monospace;
	}

	.toolbar-btn:hover {
		background: oklch(var(--b3) / 0.72);
		color: oklch(var(--bc) / 0.95);
	}
</style>
