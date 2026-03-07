<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { goto } from '$app/navigation';
	import { noteStore } from '$lib/noteStore.svelte';
	import { themeStore } from '$lib/themeStore.svelte';
	import {
		forceSimulation,
		forceLink,
		forceManyBody,
		forceCenter,
		forceCollide,
		type SimulationNodeDatum,
		type SimulationLinkDatum,
	} from 'd3-force';
	import { select } from 'd3-selection';
	import { zoom, zoomIdentity, type D3ZoomEvent } from 'd3-zoom';

	// ─── Types ───────────────────────────────────────────────────

	interface GraphNode extends SimulationNodeDatum {
		id: string;
		title: string;
		type: string;
		connectionCount: number;
	}

	interface GraphLink extends SimulationLinkDatum<GraphNode> {
		source: string | GraphNode;
		target: string | GraphNode;
	}

	// ─── Color Palette ──────────────────────────────────────────

	const NODE_COLORS: Record<string, { dark: string; light: string }> = {
		permanent: { dark: '#6b9bd2', light: '#3b6ea5' },
		literature: { dark: '#b298dc', light: '#7b5ea7' },
		fleeting:   { dark: '#e0c97f', light: '#b8973d' },
		standard:   { dark: '#8ecac1', light: '#4a9e90' },
	};

	// ─── State ───────────────────────────────────────────────────

	let canvasEl: HTMLCanvasElement | null = $state(null);
	let containerEl: HTMLDivElement | null = $state(null);
	let hoveredNode: GraphNode | null = $state(null);
	let tooltipX = $state(0);
	let tooltipY = $state(0);
	let simulation: ReturnType<typeof forceSimulation<GraphNode, GraphLink>> | null = null;
	let currentTransform = zoomIdentity;
	let nodes: GraphNode[] = $state([]);
	let links: GraphLink[] = $state([]);
	let animFrame: number | null = null;

	// ─── Graph Data ─────────────────────────────────────────────

	function buildGraph() {
		const activeNotes = noteStore.activeNotes;
		const idSet = new Set(activeNotes.map((n) => n.id));

		nodes = activeNotes.map((n) => ({
			id: n.id,
			title: n.title || n.id,
			type: n.type,
			connectionCount: n.links.filter((l) => idSet.has(l)).length +
				noteStore.getBacklinks(n.id).filter((l) => idSet.has(l)).length,
		}));

		links = [];
		const linkSet = new Set<string>();
		for (const note of activeNotes) {
			for (const target of note.links) {
				if (!idSet.has(target)) continue;
				const key = [note.id, target].sort().join('→');
				if (linkSet.has(key)) continue;
				linkSet.add(key);
				links.push({ source: note.id, target });
			}
		}
	}

	// ─── Rendering ──────────────────────────────────────────────

	function getNodeRadius(node: GraphNode): number {
		return Math.max(4, Math.min(16, 4 + node.connectionCount * 2));
	}

	function getNodeColor(type: string): string {
		const isDark = themeStore.current === 'dark';
		const palette = NODE_COLORS[type] ?? NODE_COLORS.standard;
		return isDark ? palette.dark : palette.light;
	}

	function draw() {
		if (!canvasEl) return;
		const ctx = canvasEl.getContext('2d');
		if (!ctx) return;

		const width = canvasEl.width;
		const height = canvasEl.height;
		const isDark = themeStore.current === 'dark';

		ctx.save();
		ctx.clearRect(0, 0, width, height);
		ctx.translate(currentTransform.x, currentTransform.y);
		ctx.scale(currentTransform.k, currentTransform.k);

		// Draw links
		ctx.strokeStyle = isDark ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.1)';
		ctx.lineWidth = 1;
		for (const link of links) {
			const s = link.source as GraphNode;
			const t = link.target as GraphNode;
			if (s.x == null || s.y == null || t.x == null || t.y == null) continue;
			ctx.beginPath();
			ctx.moveTo(s.x, s.y);
			ctx.lineTo(t.x, t.y);
			ctx.stroke();
		}

		// Draw nodes
		for (const node of nodes) {
			if (node.x == null || node.y == null) continue;
			const r = getNodeRadius(node);
			const color = getNodeColor(node.type);
			const isHovered = hoveredNode?.id === node.id;

			// Glow for hovered node
			if (isHovered) {
				ctx.shadowColor = color;
				ctx.shadowBlur = 12;
			}

			ctx.beginPath();
			ctx.arc(node.x, node.y, r, 0, Math.PI * 2);
			ctx.fillStyle = color;
			ctx.globalAlpha = isHovered ? 1 : 0.75;
			ctx.fill();

			ctx.shadowColor = 'transparent';
			ctx.shadowBlur = 0;
			ctx.globalAlpha = 1;

			// Label for larger/hovered nodes
			if (r >= 6 || isHovered) {
				ctx.fillStyle = isDark ? 'rgba(255,255,255,0.6)' : 'rgba(0,0,0,0.6)';
				ctx.font = `${isHovered ? '11' : '9'}px Inter, system-ui, sans-serif`;
				ctx.textAlign = 'center';
				ctx.textBaseline = 'top';

				const label = node.title.length > 24 ? node.title.slice(0, 22) + '…' : node.title;
				ctx.fillText(label, node.x, node.y + r + 3);
			}
		}

		ctx.restore();
	}

	// ─── Interaction ────────────────────────────────────────────

	function getNodeAtPoint(clientX: number, clientY: number): GraphNode | null {
		if (!canvasEl) return null;
		const rect = canvasEl.getBoundingClientRect();
		const x = (clientX - rect.left - currentTransform.x) / currentTransform.k;
		const y = (clientY - rect.top - currentTransform.y) / currentTransform.k;

		for (let i = nodes.length - 1; i >= 0; i--) {
			const node = nodes[i];
			if (node.x == null || node.y == null) continue;
			const r = getNodeRadius(node);
			const dx = x - node.x;
			const dy = y - node.y;
			if (dx * dx + dy * dy <= (r + 4) * (r + 4)) return node;
		}
		return null;
	}

	function handleMouseMove(e: MouseEvent) {
		const node = getNodeAtPoint(e.clientX, e.clientY);
		hoveredNode = node;
		tooltipX = e.clientX;
		tooltipY = e.clientY;
		if (canvasEl) canvasEl.style.cursor = node ? 'pointer' : 'grab';
		draw();
	}

	function handleClick(e: MouseEvent) {
		const node = getNodeAtPoint(e.clientX, e.clientY);
		if (node) goto(`/note/${node.id}`);
	}

	// ─── Setup ──────────────────────────────────────────────────

	function resizeCanvas() {
		if (!canvasEl || !containerEl) return;
		const rect = containerEl.getBoundingClientRect();
		const dpr = window.devicePixelRatio || 1;
		canvasEl.width = rect.width * dpr;
		canvasEl.height = rect.height * dpr;
		canvasEl.style.width = `${rect.width}px`;
		canvasEl.style.height = `${rect.height}px`;
		const ctx = canvasEl.getContext('2d');
		if (ctx) ctx.scale(dpr, dpr);
		draw();
	}

	function initSimulation() {
		if (!canvasEl || !containerEl) return;

		buildGraph();
		resizeCanvas();

		const rect = containerEl.getBoundingClientRect();
		const w = rect.width;
		const h = rect.height;

		simulation = forceSimulation<GraphNode, GraphLink>(nodes)
			.force('link', forceLink<GraphNode, GraphLink>(links)
				.id((d) => d.id)
				.distance(80)
				.strength(0.4))
			.force('charge', forceManyBody<GraphNode>().strength(-120))
			.force('center', forceCenter(w / 2, h / 2))
			.force('collide', forceCollide<GraphNode>().radius((d) => getNodeRadius(d) + 4))
			.on('tick', draw);

		// Zoom behavior
		const zoomBehavior = zoom<HTMLCanvasElement, unknown>()
			.scaleExtent([0.15, 5])
			.on('zoom', (event: D3ZoomEvent<HTMLCanvasElement, unknown>) => {
				currentTransform = event.transform;
				draw();
			});

		select(canvasEl).call(zoomBehavior);
	}

	let resizeObserver: ResizeObserver | null = null;

	onMount(() => {
		initSimulation();
		resizeObserver = new ResizeObserver(() => resizeCanvas());
		if (containerEl) resizeObserver.observe(containerEl);
	});

	onDestroy(() => {
		simulation?.stop();
		resizeObserver?.disconnect();
		if (animFrame) cancelAnimationFrame(animFrame);
	});
</script>

<div class="graph-container" bind:this={containerEl}>
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<canvas
		bind:this={canvasEl}
		onmousemove={handleMouseMove}
		onclick={handleClick}
		onmouseleave={() => { hoveredNode = null; draw(); }}
	></canvas>

	<!-- Tooltip -->
	{#if hoveredNode}
		<div
			class="graph-tooltip"
			style="left: {tooltipX + 12}px; top: {tooltipY - 32}px;"
		>
			<span class="graph-tooltip-type">{hoveredNode.type}</span>
			<span class="graph-tooltip-title">{hoveredNode.title}</span>
			<span class="graph-tooltip-links">{hoveredNode.connectionCount} connections</span>
		</div>
	{/if}

	<!-- Legend -->
	<div class="graph-legend">
		{#each Object.entries(NODE_COLORS) as [type, colors]}
			<div class="graph-legend-item">
				<span
					class="graph-legend-dot"
					style="background: {themeStore.current === 'dark' ? colors.dark : colors.light}"
				></span>
				<span class="graph-legend-label">{type}</span>
			</div>
		{/each}
	</div>

	<!-- Stats -->
	<div class="graph-stats">
		{nodes.length} nodes · {links.length} connections
	</div>
</div>

<style>
	.graph-container {
		position: relative;
		width: 100%;
		height: 100%;
		min-height: 400px;
		overflow: hidden;
		border-radius: var(--rounded-box);
	}

	canvas {
		display: block;
		width: 100%;
		height: 100%;
	}

	.graph-tooltip {
		position: fixed;
		z-index: 100;
		padding: 6px 10px;
		border-radius: 6px;
		pointer-events: none;
		display: flex;
		flex-direction: column;
		gap: 2px;
		backdrop-filter: blur(12px);
		-webkit-backdrop-filter: blur(12px);
		background: rgba(18, 18, 18, 0.92);
		border: 1px solid rgba(255, 255, 255, 0.08);
		box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
	}

	:global([data-theme="light"]) .graph-tooltip {
		background: rgba(255, 255, 255, 0.95);
		border: 1px solid rgba(0, 0, 0, 0.1);
		box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
	}

	.graph-tooltip-type {
		font-size: 10px;
		text-transform: capitalize;
		opacity: 0.4;
	}

	.graph-tooltip-title {
		font-size: 12px;
		font-weight: 600;
	}

	.graph-tooltip-links {
		font-size: 10px;
		opacity: 0.5;
	}

	.graph-legend {
		position: absolute;
		bottom: 12px;
		left: 12px;
		display: flex;
		flex-direction: column;
		gap: 4px;
		padding: 8px 10px;
		border-radius: 8px;
		background: rgba(18, 18, 18, 0.7);
		backdrop-filter: blur(8px);
		-webkit-backdrop-filter: blur(8px);
		border: 1px solid rgba(255, 255, 255, 0.06);
	}

	:global([data-theme="light"]) .graph-legend {
		background: rgba(255, 255, 255, 0.8);
		border: 1px solid rgba(0, 0, 0, 0.08);
	}

	.graph-legend-item {
		display: flex;
		align-items: center;
		gap: 6px;
	}

	.graph-legend-dot {
		width: 8px;
		height: 8px;
		border-radius: 50%;
		flex-shrink: 0;
	}

	.graph-legend-label {
		font-size: 10px;
		text-transform: capitalize;
		opacity: 0.6;
	}

	.graph-stats {
		position: absolute;
		bottom: 12px;
		right: 12px;
		font-size: 11px;
		opacity: 0.3;
		padding: 4px 8px;
		border-radius: 6px;
		background: rgba(18, 18, 18, 0.5);
		backdrop-filter: blur(8px);
		-webkit-backdrop-filter: blur(8px);
	}

	:global([data-theme="light"]) .graph-stats {
		background: rgba(255, 255, 255, 0.6);
	}
</style>
