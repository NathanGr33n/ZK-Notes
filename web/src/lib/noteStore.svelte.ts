import type { Note, NoteType, NoteColor, SortMode } from './types';
import { createNote } from './types';
import { extractLinkTargets } from './linkParser';
import { extractTags } from './tagParser';
import * as storage from './storage';

/**
 * Central reactive store for all Zettelkasten notes.
 * Uses Svelte 5 runes ($state, $derived) for fine-grained reactivity.
 * Persistence is backed by IndexedDB via the storage adapter.
 */
function createNoteStore() {
	let notes = $state<Note[]>([]);
	let counter = $state<number>(0);
	let ready = $state(false);
	let _readyResolve: (() => void) | null = null;
	const readyPromise = new Promise<void>((r) => { _readyResolve = r; });

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

	/** All unique tags across every non-archived note. */
	const allTags = $derived.by<string[]>(() => {
		const tagSet = new Set<string>();
		for (const note of notes) {
			if (note.archived) continue;
			for (const tag of note.tags) tagSet.add(tag);
		}
		return [...tagSet].sort();
	});

	/** Active (non-archived) notes. */
	const activeNotes = $derived(notes.filter((n) => !n.archived));

	/** Pinned notes, most recently edited first. */
	const pinnedNotes = $derived(
		activeNotes.filter((n) => n.pinned).sort((a, b) => b.lastEdit.localeCompare(a.lastEdit))
	);

	/** Archived notes. */
	const archivedNotes = $derived(notes.filter((n) => n.archived));

	// --- Async initialization ---

	async function init(): Promise<void> {
		if (ready) return;
		try {
			// Migrate legacy localStorage data (runs once, no-op if already migrated)
			await storage.migrateFromLocalStorage();

			const loaded = await storage.loadAll();
			notes = loaded.map((n) => ({
				...n,
				pinned: n.pinned ?? false,
				archived: n.archived ?? false,
				color: n.color ?? 'none',
			}));
			counter = await storage.loadCounter();
		} catch (err) {
			console.error('[NoteStore] Failed to load from IndexedDB:', err);
		}
		ready = true;
		_readyResolve?.();
	}

	// --- Actions ---

	function generateId(): string {
		counter++;
		storage.saveCounter(counter); // fire-and-forget
		return `ZK-${String(counter).padStart(4, '0')}`;
	}

	function add(type: NoteType = 'permanent'): Note {
		const note = createNote(generateId(), type);
		notes = [...notes, note];
		storage.saveOne(note); // fire-and-forget
		return note;
	}

	function update(id: string, changes: Partial<Omit<Note, 'id' | 'created'>>) {
		let updatedNote: Note | null = null;
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

			updatedNote = updated;
			return updated;
		});
		if (updatedNote) storage.saveOne(updatedNote); // fire-and-forget
	}

	function remove(id: string) {
		notes = notes.filter((n) => n.id !== id);
		storage.removeOne(id); // fire-and-forget
	}

	/** Toggle pin status. */
	function togglePin(id: string) {
		const note = byId.get(id);
		if (note) update(id, { pinned: !note.pinned });
	}

	/** Soft-delete: move to archive. */
	function archive(id: string) {
		update(id, { archived: true });
	}

	/** Restore from archive. */
	function restore(id: string) {
		update(id, { archived: false });
	}

	/** Set the accent color of a note. */
	function setColor(id: string, color: NoteColor) {
		update(id, { color });
	}

	/** Sort a list of notes by the given mode. */
	function sorted(list: Note[], mode: SortMode): Note[] {
		return [...list].sort((a, b) => {
			switch (mode) {
				case 'lastEdit': return b.lastEdit.localeCompare(a.lastEdit);
				case 'created': return b.created.localeCompare(a.created);
				case 'alpha': return (a.title || a.id).localeCompare(b.title || b.id);
			}
		});
	}

	/** Export all notes as a JSON string. */
	function exportNotes(): string {
		return JSON.stringify(notes, null, 2);
	}

	/** Import notes from JSON, merging by ID. */
	function importNotes(json: string) {
		try {
			const imported: Note[] = JSON.parse(json);
			const existing = new Set(notes.map((n) => n.id));
			const newNotes = imported.filter((n) => !existing.has(n.id));
			const normalized = newNotes.map((n) => ({
				...n,
				pinned: n.pinned ?? false,
				archived: n.archived ?? false,
				color: n.color ?? 'none',
			}));
			notes = [...notes, ...normalized];
			storage.saveAll(notes); // fire-and-forget full write
		} catch { /* invalid JSON — fail silently */ }
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

	return {
		get notes() { return notes; },
		get activeNotes() { return activeNotes; },
		get pinnedNotes() { return pinnedNotes; },
		get archivedNotes() { return archivedNotes; },
		get allTags() { return allTags; },
		get ready() { return ready; },
		get readyPromise() { return readyPromise; },
		init,
		add,
		update,
		remove,
		getById,
		getBacklinks,
		search,
		togglePin,
		archive,
		restore,
		setColor,
		sorted,
		exportNotes,
		importNotes,
	};
}

export const noteStore = createNoteStore();
