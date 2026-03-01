/**
 * Extracts #hashtags from note content.
 * Ignores tags inside code blocks, inline code, and URLs.
 */

// Matches #tag where tag is word characters (letters, digits, underscore, hyphen)
// but NOT preceded by & (HTML entities) or inside a URL path.
const TAG_PATTERN = /(?:^|(?<=\s))#([a-zA-Z][\w-]*)/g;

/** Extract all unique #hashtags from content as lowercase strings. */
export function extractTags(content: string): string[] {
	if (!content) return [];

	// Strip code blocks and inline code before extracting
	const stripped = content
		.replace(/```[\s\S]*?```/g, '')   // fenced code blocks
		.replace(/`[^`]+`/g, '');          // inline code

	const tags = new Set<string>();
	for (const match of stripped.matchAll(TAG_PATTERN)) {
		tags.add(match[1].toLowerCase());
	}
	return [...tags];
}
