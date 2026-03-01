/**
 * Extracts and transforms [[wiki-style links]] in note content.
 * Supports both [[target]] and [[target|display text]] syntax.
 */

const LINK_PATTERN = /\[\[([^\]]+)\]\]/g;

/** Parsed representation of a single wiki link. */
export interface ParsedLink {
	target: string;  // The note ID or title being linked to
	display: string; // The visible text (may differ from target with |alias)
	raw: string;     // The full [[...]] match
}

/** Extract all [[wiki-links]] from content. */
export function extractLinks(content: string): ParsedLink[] {
	if (!content) return [];

	const links: ParsedLink[] = [];
	for (const match of content.matchAll(LINK_PATTERN)) {
		const inner = match[1].trim();
		if (!inner) continue;

		const parts = inner.split('|', 2);
		links.push({
			target: parts[0].trim(),
			display: (parts[1] ?? parts[0]).trim(),
			raw: match[0],
		});
	}
	return links;
}

/** Extract just the link target strings. */
export function extractLinkTargets(content: string): string[] {
	return extractLinks(content).map((l) => l.target);
}

/**
 * Replace [[wiki-links]] with HTML anchor tags for rendering.
 * Internal links get a `data-note-link` attribute; external URLs
 * get `target="_blank"`.
 */
export function replaceLinksWithHtml(content: string): string {
	if (!content) return '';

	return content.replace(LINK_PATTERN, (_match, inner: string) => {
		const trimmed = inner.trim();
		if (!trimmed) return '';

		const parts = trimmed.split('|', 2);
		const target = parts[0].trim();
		const display = (parts[1] ?? parts[0]).trim();

		return `<a href="#" data-note-link="${encodeURIComponent(target)}" class="link link-primary">${escapeHtml(display)}</a>`;
	});
}

function escapeHtml(str: string): string {
	return str
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;');
}
