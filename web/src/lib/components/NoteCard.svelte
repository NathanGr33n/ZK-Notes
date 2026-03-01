<script lang="ts">
	import type { Note } from '$lib/types';
	import { fade } from 'svelte/transition';
	import { onMount } from 'svelte';
	import gsap from 'gsap';

	interface Props {
		note: Note;
	}

	const { note }: Props = $props();

	const typeColors: Record<string, string> = {
		fleeting: 'badge-warning',
		literature: 'badge-info',
		permanent: 'badge-success',
		standard: 'badge-neutral',
	};

	/** First ~120 chars of content as a preview snippet. */
	const snippet = $derived(
		note.content.length > 120 ? note.content.slice(0, 120) + '…' : note.content || 'Empty note'
	);

	let cardEl: HTMLElement | null = $state(null);

	onMount(() => {
		if (cardEl) {
			gsap.from(cardEl, {
				opacity: 0,
				y: 12,
				duration: 0.3,
				ease: 'power2.out',
			});
		}
	});

	/** Relative time label. */
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
	class="card bg-base-100 border border-base-300 shadow-sm hover:shadow-md
		hover:-translate-y-0.5 transition-all duration-150 cursor-pointer"
>
	<div class="card-body p-4 gap-2">
		<!-- Header row: type badge + ID -->
		<div class="flex items-center justify-between">
			<span class="badge {typeColors[note.type] ?? 'badge-neutral'} badge-sm capitalize">
				{note.type}
			</span>
			<span class="text-xs opacity-40 font-mono">{note.id}</span>
		</div>

		<!-- Title -->
		<h3 class="card-title text-sm leading-tight line-clamp-2">
			{note.title || 'Untitled'}
		</h3>

		<!-- Snippet -->
		<p class="text-xs opacity-60 line-clamp-3">{snippet}</p>

		<!-- Tags -->
		{#if note.tags.length > 0}
			<div class="flex flex-wrap gap-1">
				{#each note.tags.slice(0, 5) as tag}
					<span class="badge badge-outline badge-xs">#{tag}</span>
				{/each}
				{#if note.tags.length > 5}
					<span class="badge badge-ghost badge-xs">+{note.tags.length - 5}</span>
				{/if}
			</div>
		{/if}

		<!-- Footer: links count + timestamp -->
		<div class="flex items-center justify-between text-xs opacity-40 pt-1">
			<span>
				{note.links.length > 0 ? `${note.links.length} link${note.links.length > 1 ? 's' : ''}` : ''}
			</span>
			<span>{timeAgo(note.lastEdit)}</span>
		</div>
	</div>
</a>
