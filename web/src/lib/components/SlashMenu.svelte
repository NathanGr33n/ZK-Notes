<script lang="ts">
	import { fade } from 'svelte/transition';

	interface Props {
		textarea: HTMLTextAreaElement | null;
		onInsert: (newContent: string) => void;
	}

	const { textarea, onInsert }: Props = $props();

	let visible = $state(false);
	let query = $state('');
	let selectedIndex = $state(0);
	let posX = $state(0);
	let posY = $state(0);

	interface SlashItem {
		label: string;
		icon: string;
		block: string;
	}

	const items: SlashItem[] = [
		{ label: 'Heading 1', icon: 'H1', block: '# ' },
		{ label: 'Heading 2', icon: 'H2', block: '## ' },
		{ label: 'Heading 3', icon: 'H3', block: '### ' },
		{ label: 'Bullet List', icon: '•', block: '- ' },
		{ label: 'Numbered List', icon: '1.', block: '1. ' },
		{ label: 'Quote', icon: '"', block: '> ' },
		{ label: 'Code Block', icon: '```', block: '```\n\n```' },
		{ label: 'Divider', icon: '—', block: '---' },
		{ label: 'Callout', icon: '💡', block: '> **Note:** ' },
		{ label: 'Wiki Link', icon: '🔗', block: '[[' },
	];

	const filteredItems = $derived.by(() => {
		if (!query) return items;
		const q = query.toLowerCase();
		return items.filter((i) => i.label.toLowerCase().includes(q));
	});

	/** Called on textarea input to detect / trigger. */
	export function handleInput() {
		if (!textarea) return;
		const val = textarea.value;
		const pos = textarea.selectionStart;

		// Look for / at start of line
		const before = val.slice(0, pos);
		const lastNewline = before.lastIndexOf('\n');
		const lineStart = lastNewline + 1;
		const lineContent = before.slice(lineStart);

		if (lineContent.startsWith('/')) {
			query = lineContent.slice(1);
			visible = true;
			selectedIndex = 0;

			const rect = textarea.getBoundingClientRect();
			posX = rect.left + 16;
			posY = rect.top + 32;
		} else {
			visible = false;
			query = '';
		}
	}

	/** Handle keyboard navigation. Returns true if handled. */
	export function handleKeydown(e: KeyboardEvent): boolean {
		if (!visible || filteredItems.length === 0) return false;

		if (e.key === 'ArrowDown') {
			e.preventDefault();
			selectedIndex = (selectedIndex + 1) % filteredItems.length;
			return true;
		}
		if (e.key === 'ArrowUp') {
			e.preventDefault();
			selectedIndex = (selectedIndex - 1 + filteredItems.length) % filteredItems.length;
			return true;
		}
		if (e.key === 'Enter' || e.key === 'Tab') {
			e.preventDefault();
			insertItem(filteredItems[selectedIndex]);
			return true;
		}
		if (e.key === 'Escape') {
			e.preventDefault();
			visible = false;
			return true;
		}
		return false;
	}

	function insertItem(item: SlashItem) {
		if (!textarea) return;
		const val = textarea.value;
		const pos = textarea.selectionStart;
		const before = val.slice(0, pos);
		const lastNewline = before.lastIndexOf('\n');
		const lineStart = lastNewline + 1;

		const newVal = val.slice(0, lineStart) + item.block + val.slice(pos);
		onInsert(newVal);
		visible = false;
		query = '';

		requestAnimationFrame(() => {
			if (!textarea) return;
			const newPos = lineStart + item.block.length;
			textarea.selectionStart = newPos;
			textarea.selectionEnd = newPos;
			textarea.focus();
		});
	}
</script>

{#if visible && filteredItems.length > 0}
	<div
		class="slash-menu"
		style="left: {posX}px; top: {posY}px;"
		transition:fade={{ duration: 80 }}
	>
		{#each filteredItems as item, i}
			<button
				class="slash-item"
				class:slash-item-active={i === selectedIndex}
				onmouseenter={() => (selectedIndex = i)}
				onclick={() => insertItem(item)}
			>
				<span class="slash-icon">{item.icon}</span>
				<span>{item.label}</span>
			</button>
		{/each}
	</div>
{/if}

<style>
	.slash-menu {
		position: fixed;
		z-index: 60;
		width: 200px;
		background: rgba(18, 18, 18, 0.96);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: 8px;
		backdrop-filter: blur(16px);
		-webkit-backdrop-filter: blur(16px);
		box-shadow: 0 12px 32px rgba(0, 0, 0, 0.4);
		padding: 4px;
		max-height: 280px;
		overflow-y: auto;
	}

	.slash-item {
		display: flex;
		align-items: center;
		gap: 8px;
		width: 100%;
		padding: 6px 10px;
		border: none;
		background: transparent;
		border-radius: 6px;
		color: rgba(255, 255, 255, 0.7);
		font-size: 12px;
		cursor: pointer;
		text-align: left;
		transition: background 0.08s ease;
	}

	.slash-item-active {
		background: rgba(255, 255, 255, 0.06);
		color: rgba(255, 255, 255, 0.95);
	}

	.slash-icon {
		font-size: 11px;
		width: 20px;
		text-align: center;
		flex-shrink: 0;
		font-family: ui-monospace, monospace;
		font-weight: 600;
		color: rgba(255, 255, 255, 0.35);
	}
</style>
