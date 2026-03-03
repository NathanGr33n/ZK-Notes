<script lang="ts">
	import type { Note } from '$lib/types';
	import { onMount } from 'svelte';
	import gsap from 'gsap';

	interface Props {
		note: Note;
		size?: 'normal' | 'featured';
	}

	const { note, size = 'normal' }: Props = $props();

	const typeIcons: Record<string, string> = {
		fleeting: '⚡',
		literature: '📖',
		permanent: '📝',
		standard: '📄',
	};

	const snippetLength = $derived(size === 'featured' ? 200 : 100);
	const snippet = $derived(
		note.content.length > snippetLength
			? note.content.slice(0, snippetLength) + '…'
			: note.content || 'Empty note'
	);

	const accentClass = $derived(`note-accent-${note.color ?? 'none'}`);

	let cardEl: HTMLElement | null = $state(null);

	onMount(() => {
		if (cardEl) {
			gsap.from(cardEl, {
				opacity: 0,
				y: 10,
				duration: 0.25,
				ease: 'power2.out',
			});
		}
	});

	function timeAgo(iso: string): string {
		const diff = Date.now() - new Date(iso).getTime();
		const mins = Math.floor(diff / 60_000);
		if (mins < 1) return 'just now';
		if (mins < 60) return `${mins}m ago`;
		const hours = Math.floor(mins / 60);
		if (hours < 24) return `${hours}h ago`;
		const days = Math.floor(hours / 24);
		return `${days}d ago`;
	}
</script>

<a
	bind:this={cardEl}
	href="/note/{note.id}"
	class="note-card surface {accentClass}"
	class:note-card-featured={size === 'featured'}
>
	<div class="note-card-body">
		<!-- Header -->
		<div class="flex items-center gap-2">
			<span class="note-type-icon">{typeIcons[note.type] ?? '📄'}</span>
			<span class="note-type-label">{note.type}</span>
			{#if note.pinned}
				<span class="note-pin" title="Pinned">📌</span>
			{/if}
			<span class="note-id">{note.id}</span>
		</div>

		<!-- Title -->
		<h3 class="note-title">
			{note.title || 'Untitled'}
		</h3>

		<!-- Snippet -->
		<p class="note-snippet">{snippet}</p>

		<!-- Tags -->
		{#if note.tags.length > 0}
			<div class="note-tags">
				{#each note.tags.slice(0, 4) as tag}
					<span class="note-tag">#{tag}</span>
				{/each}
				{#if note.tags.length > 4}
					<span class="note-tag note-tag-more">+{note.tags.length - 4}</span>
				{/if}
			</div>
		{/if}

		<!-- Footer -->
		<div class="note-footer">
			{#if note.links.length > 0}
				<span>{note.links.length} link{note.links.length > 1 ? 's' : ''}</span>
			{:else}
				<span></span>
			{/if}
			<span>{timeAgo(note.lastEdit)}</span>
		</div>
	</div>
</a>

<style>
	.note-card {
		display: block;
		padding: 0;
		cursor: pointer;
		transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
	}

	.note-card:hover {
		transform: translateY(-2px);
		box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
		border-color: rgba(255, 255, 255, 0.1);
	}

	.note-card-featured {
		grid-column: span 2;
	}

	.note-card-body {
		padding: 14px 16px;
		display: flex;
		flex-direction: column;
		gap: 8px;
	}

	.note-type-icon {
		font-size: 12px;
	}

	.note-type-label {
		font-size: 11px;
		text-transform: capitalize;
		color: rgba(255, 255, 255, 0.35);
		font-weight: 500;
	}

	.note-pin {
		font-size: 11px;
	}

	.note-id {
		margin-left: auto;
		font-size: 10px;
		font-family: ui-monospace, monospace;
		color: rgba(255, 255, 255, 0.2);
	}

	.note-title {
		font-size: 14px;
		font-weight: 600;
		line-height: 1.3;
		color: rgba(255, 255, 255, 0.85);
		overflow: hidden;
		display: -webkit-box;
		-webkit-line-clamp: 2;
		line-clamp: 2;
		-webkit-box-orient: vertical;
		letter-spacing: -0.01em;
	}

	.note-snippet {
		font-size: 12px;
		line-height: 1.5;
		color: rgba(255, 255, 255, 0.4);
		overflow: hidden;
		display: -webkit-box;
		-webkit-line-clamp: 3;
		line-clamp: 3;
		-webkit-box-orient: vertical;
	}

	.note-card-featured .note-snippet {
		-webkit-line-clamp: 5;
		line-clamp: 5;
	}

	.note-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 4px;
	}

	.note-tag {
		padding: 1px 6px;
		font-size: 10px;
		border-radius: 4px;
		background: rgba(255, 255, 255, 0.04);
		color: rgba(255, 255, 255, 0.35);
		border: 1px solid rgba(255, 255, 255, 0.06);
	}

	.note-tag-more {
		color: rgba(255, 255, 255, 0.2);
	}

	.note-footer {
		display: flex;
		justify-content: space-between;
		font-size: 11px;
		color: rgba(255, 255, 255, 0.2);
		padding-top: 4px;
	}

	/* Light theme overrides */
	:global([data-theme="light"]) .note-title {
		color: rgba(0, 0, 0, 0.85);
	}
	:global([data-theme="light"]) .note-snippet {
		color: rgba(0, 0, 0, 0.5);
	}
	:global([data-theme="light"]) .note-type-label {
		color: rgba(0, 0, 0, 0.4);
	}
	:global([data-theme="light"]) .note-id {
		color: rgba(0, 0, 0, 0.25);
	}
	:global([data-theme="light"]) .note-tag {
		background: rgba(0, 0, 0, 0.04);
		color: rgba(0, 0, 0, 0.45);
		border-color: rgba(0, 0, 0, 0.08);
	}
	:global([data-theme="light"]) .note-footer {
		color: rgba(0, 0, 0, 0.3);
	}
	:global([data-theme="light"]) .note-card:hover {
		box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
		border-color: rgba(0, 0, 0, 0.15);
	}
</style>
