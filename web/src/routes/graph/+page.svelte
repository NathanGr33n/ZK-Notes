<script lang="ts">
	import { noteStore } from '$lib/noteStore.svelte';
	import GraphView from '$lib/components/GraphView.svelte';
</script>

<div class="flex flex-col h-full gap-4">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<div class="flex items-center gap-3">
			<a href="/" class="btn btn-ghost btn-sm">&larr; Back</a>
			<h1 class="text-base font-semibold opacity-80">Knowledge Graph</h1>
		</div>
		<span class="text-xs opacity-30">Scroll to zoom · Drag to pan · Click node to open</span>
	</div>

	<!-- Graph -->
	{#if noteStore.activeNotes.length === 0}
		<div class="flex flex-col items-center justify-center flex-1">
			<div class="surface p-8 text-center max-w-md">
				<div class="text-4xl mb-4">🔗</div>
				<h3 class="text-lg font-semibold mb-2 opacity-80">No notes to graph</h3>
				<p class="text-sm opacity-40 mb-4">
					Create some notes and link them with [[wiki-links]] to see your knowledge graph.
				</p>
				<a href="/" class="btn btn-primary btn-sm">Go to workspace</a>
			</div>
		</div>
	{:else}
		<div class="flex-1 surface overflow-hidden">
			<GraphView />
		</div>
	{/if}
</div>
