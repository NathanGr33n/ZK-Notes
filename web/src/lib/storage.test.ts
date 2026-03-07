import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import 'fake-indexeddb/auto';
import { createNote } from './types';
import type { Note } from './types';
import {
	loadAll,
	saveOne,
	saveAll,
	removeOne,
	loadCounter,
	saveCounter,
	migrateFromLocalStorage,
	_resetForTesting,
} from './storage';

// ─── localStorage polyfill for Node ─────────────────────────────

const store = new Map<string, string>();
Object.defineProperty(globalThis, 'localStorage', {
	value: {
		getItem: (key: string) => store.get(key) ?? null,
		setItem: (key: string, value: string) => store.set(key, value),
		removeItem: (key: string) => store.delete(key),
		clear: () => store.clear(),
	},
	writable: true,
});

// ─── Helpers ────────────────────────────────────────────────────

function makeNote(id: string, overrides?: Partial<Note>): Note {
	return { ...createNote(id, 'permanent'), ...overrides };
}

// ─── Tests ──────────────────────────────────────────────────────

describe('storage: CRUD operations', () => {
	beforeEach(async () => {
		_resetForTesting();
		const dbs = await indexedDB.databases();
		for (const db of dbs) {
			if (db.name) indexedDB.deleteDatabase(db.name);
		}
	});

	afterEach(() => {
		_resetForTesting();
	});

	it('loadAll returns empty array from fresh DB', async () => {
		const notes = await loadAll();
		expect(notes).toEqual([]);
	});

	it('saveOne + loadAll round-trips a note', async () => {
		const note = makeNote('ZK-0001', { title: 'Test Note' });
		await saveOne(note);

		const all = await loadAll();
		expect(all).toHaveLength(1);
		expect(all[0].id).toBe('ZK-0001');
		expect(all[0].title).toBe('Test Note');
	});

	it('saveOne upserts (updates existing note)', async () => {
		const note = makeNote('ZK-0001', { title: 'Original' });
		await saveOne(note);

		await saveOne({ ...note, title: 'Updated' });
		const all = await loadAll();
		expect(all).toHaveLength(1);
		expect(all[0].title).toBe('Updated');
	});

	it('saveAll overwrites all notes', async () => {
		await saveOne(makeNote('ZK-0001'));
		await saveOne(makeNote('ZK-0002'));

		const newNotes = [makeNote('ZK-0003'), makeNote('ZK-0004')];
		await saveAll(newNotes);

		const all = await loadAll();
		expect(all).toHaveLength(2);
		expect(all.map((n) => n.id).sort()).toEqual(['ZK-0003', 'ZK-0004']);
	});

	it('removeOne deletes a specific note', async () => {
		await saveOne(makeNote('ZK-0001'));
		await saveOne(makeNote('ZK-0002'));

		await removeOne('ZK-0001');
		const all = await loadAll();
		expect(all).toHaveLength(1);
		expect(all[0].id).toBe('ZK-0002');
	});

	it('removeOne is a no-op for non-existent ID', async () => {
		await saveOne(makeNote('ZK-0001'));
		await removeOne('NONEXISTENT');
		const all = await loadAll();
		expect(all).toHaveLength(1);
	});
});

describe('storage: counter', () => {
	beforeEach(async () => {
		_resetForTesting();
		const dbs = await indexedDB.databases();
		for (const db of dbs) {
			if (db.name) indexedDB.deleteDatabase(db.name);
		}
	});

	afterEach(() => {
		_resetForTesting();
	});

	it('loadCounter returns 0 from fresh DB', async () => {
		expect(await loadCounter()).toBe(0);
	});

	it('saveCounter + loadCounter round-trips', async () => {
		await saveCounter(42);
		expect(await loadCounter()).toBe(42);
	});

	it('saveCounter overwrites previous value', async () => {
		await saveCounter(10);
		await saveCounter(20);
		expect(await loadCounter()).toBe(20);
	});
});

describe('storage: migrateFromLocalStorage', () => {
	beforeEach(async () => {
		store.clear();
		_resetForTesting();
		const dbs = await indexedDB.databases();
		for (const db of dbs) {
			if (db.name) indexedDB.deleteDatabase(db.name);
		}
	});

	afterEach(() => {
		store.clear();
		_resetForTesting();
	});

	it('no-ops when localStorage is empty', async () => {
		await migrateFromLocalStorage();
		expect(await loadAll()).toEqual([]);
	});

	it('migrates notes from localStorage to IndexedDB', async () => {
		const notes = [
			makeNote('ZK-0001', { title: 'Note A' }),
			makeNote('ZK-0002', { title: 'Note B' }),
		];
		localStorage.setItem('zk-notes', JSON.stringify(notes));
		localStorage.setItem('zk-counter', '2');

		await migrateFromLocalStorage();

		const all = await loadAll();
		expect(all).toHaveLength(2);
		expect(await loadCounter()).toBe(2);
		expect(localStorage.getItem('zk-notes')).toBeNull();
		expect(localStorage.getItem('zk-counter')).toBeNull();
	});

	it('fills in missing fields during migration', async () => {
		const oldNotes = [{
			id: 'ZK-0001', title: 'Old', content: '', type: 'permanent',
			tags: [], links: [], backlinks: [], created: '2024-01-01', lastEdit: '2024-01-01',
		}];
		localStorage.setItem('zk-notes', JSON.stringify(oldNotes));

		await migrateFromLocalStorage();

		const all = await loadAll();
		expect(all[0].pinned).toBe(false);
		expect(all[0].archived).toBe(false);
		expect(all[0].color).toBe('none');
	});

	it('cleans up localStorage even for empty array', async () => {
		localStorage.setItem('zk-notes', '[]');
		await migrateFromLocalStorage();
		expect(localStorage.getItem('zk-notes')).toBeNull();
	});
});
