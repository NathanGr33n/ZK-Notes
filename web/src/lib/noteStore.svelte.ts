import type { Note, NoteType } from './types';
import { createNote } from './types';
import { extractLinkTargets } from './linkParser';
import { extractTags } from './tagParser';

const STORAGE_KEY = 'zk-notes';
const COUNTER_KEY = 'zk-counter';

/**
 * Central reactive store for all Zettelkasten notes.
 * Uses Svelte 5 runes ($state, $derived) for fine-grained reactivity.
 */
function createNoteStore() {
	let notes = $state<Note[]>(loadFromStorage());
	let counter = $state<number>(loadCounter());

	// --- Derived indexes ---

	/** Map of noteId → Note for O(1) lookup. */
	const byId = $derived<Map<string, Note>>(
		new Map(notes.map((n) => [n.id, n]))
	);

	/**
	 * Backlink index: noteId → array of IDs that link TO it.
	 * Auto-recomputed whenever notes change (METHODOLOGY.md §5.2).
	 */
	const backlinkIndex = $derived.by<Map<string, string[]>>(() => {
		const index = new Map<string, string[]>();
		for (const note of notes) {
			for (const target of note.links) {
				const existing = index.get(target) ?? [];
				existing.push(note.id);
				index.set(target, existing);
			}
		}
		return index;
	});

	/** All unique tags across all notes. */
	const allTags = $derived.by<string[]>(() => {
		const tagSet = new Set<string>();
		for (const note of notes) {
			for (const tag of note.tags) tagSet.add(tag);
		}
		return [...tagSet].sort();
	});

	// --- Actions ---

	function generateId(): string {
		counter++;
		saveCounter(counter);
		return `ZK-${String(counter).padStart(4, '0')}`;
	}

	function add(type: NoteType = 'permanent'): Note {
		const note = createNote(generateId(), type);
		notes = [...notes, note];
		persist();
		return note;
	}

	function update(id: string, changes: Partial<Omit<Note, 'id' | 'created'>>) {
		notes = notes.map((n) => {
			if (n.id !== id) return n;

			const updated = { ...n, ...changes, lastEdit: new Date().toISOString() };

			// Re-extract links and tags from content when content changes
			if (changes.content !== undefined) {
				updated.links = extractLinkTargets(updated.content);
				updated.tags = [
					...new Set([
						...updated.tags.filter((t) => !t.startsWith('#')),
						...extractTags(updated.content),
					]),
				];
			}

			return updated;
		});
		persist();
	}

	function remove(id: string) {
		notes = notes.filter((n) => n.id !== id);
		persist();
	}

	function getById(id: string): Note | undefined {
		return byId.get(id);
	}

	function getBacklinks(id: string): string[] {
		return backlinkIndex.get(id) ?? [];
	}

	function search(query: string): Note[] {
		if (!query.trim()) return notes;
		const q = query.toLowerCase();
		return notes.filter(
			(n) =>
				n.title.toLowerCase().includes(q) ||
				n.content.toLowerCase().includes(q) ||
				n.tags.some((t) => t.includes(q))
		);
	}

	// --- Persistence ---

	function persist() {
		try {
			localStorage.setItem(STORAGE_KEY, JSON.stringify(notes));
		} catch {
			// Storage full or unavailable — fail silently
		}
	}

	return {
		get notes() { return notes; },
		get allTags() { return allTags; },
		add,
		update,
		remove,
		getById,
		getBacklinks,
		search,
	};
}

function loadFromStorage(): Note[] {
	if (typeof localStorage === 'undefined') return [];
	try {
		const raw = localStorage.getItem(STORAGE_KEY);
		return raw ? JSON.parse(raw) : [];
	} catch {
		return [];
	}
}

function loadCounter(): number {
	if (typeof localStorage === 'undefined') return 0;
	try {
		return parseInt(localStorage.getItem(COUNTER_KEY) ?? '0', 10);
	} catch {
		return 0;
	}
}

function saveCounter(value: number) {
	try {
		localStorage.setItem(COUNTER_KEY, String(value));
	} catch {
		// fail silently
	}
}

export const noteStore = createNoteStore();
