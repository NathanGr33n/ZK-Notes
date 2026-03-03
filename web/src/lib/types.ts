/**
 * Zettelkasten note types per METHODOLOGY.md §3.
 *
 * - Fleeting:   Quick temporary captures (§3.1)
 * - Literature: Single ideas from a source, rewritten in own words (§3.2)
 * - Permanent:  Atomic, autonomous, linked notes — the backbone (§3.3)
 * - Standard:   General-purpose note (backward compat with existing data)
 */
export type NoteType = 'fleeting' | 'literature' | 'permanent' | 'standard';

/**
 * Link relationship types per METHODOLOGY.md §4.
 * Links are claims about how ideas relate.
 */
export type LinkRelation = 'continuation' | 'contrast' | 'example' | 'contextual';

/** Available accent colors for note cover strips. */
export const NOTE_COLORS = ['none', 'blue', 'violet', 'teal', 'amber', 'rose', 'sage', 'stone'] as const;
export type NoteColor = (typeof NOTE_COLORS)[number];

/** Sort options for note lists. */
export type SortMode = 'lastEdit' | 'created' | 'alpha';

/** YAML frontmatter metadata stored at the top of each .md file. */
export interface NoteMetadata {
	id: string;
	title: string;
	type: NoteType;
	tags: string[];
	links: string[];
	created: string;   // ISO 8601
	lastEdit: string;  // ISO 8601
	lastReviewed?: string;
	pinned?: boolean;
	archived?: boolean;
	color?: NoteColor;
}

/** A fully hydrated Zettelkasten note. */
export interface Note {
	id: string;
	title: string;
	content: string;
	type: NoteType;
	tags: string[];
	links: string[];       // Forward links (IDs or titles)
	backlinks: string[];   // Computed: notes that link TO this note
	created: string;
	lastEdit: string;
	lastReviewed?: string;
	pinned: boolean;
	archived: boolean;
	color: NoteColor;
}

/** Creates a blank note with sensible defaults. */
export function createNote(id: string, type: NoteType = 'permanent'): Note {
	const now = new Date().toISOString();
	return {
		id,
		title: '',
		content: '',
		type,
		tags: [],
		links: [],
		backlinks: [],
		created: now,
		lastEdit: now,
		pinned: false,
		archived: false,
		color: 'none',
	};
}
