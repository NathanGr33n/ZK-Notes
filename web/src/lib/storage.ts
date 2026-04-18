/**
 * IndexedDB-backed storage adapter for Zettelkasten notes.
 *
 * Provides async CRUD operations, diagnostics, and local
 * operational metadata with automatic migration from
 * localStorage on first load. Uses the `idb` library for a
 * promise-based IndexedDB API.
 *
 * DB schema:
 *   - Object store "notes"   — keyed by note `id`
 *   - Object store "meta"    — single key "counter" for ID generation
 */

import { openDB, type IDBPDatabase } from 'idb';
import type { Note } from './types';

const DB_NAME = 'zk-notes-db';
const DB_VERSION = 1;
const NOTES_STORE = 'notes';
const META_STORE = 'meta';
const STORAGE_INFO_KEY = 'storage-info';

// Legacy localStorage keys (for migration)
const LS_NOTES_KEY = 'zk-notes';
const LS_COUNTER_KEY = 'zk-counter';

let dbInstance: IDBPDatabase | null = null;

interface StorageInfo {
	migratedFromLocalStorage: boolean;
	lastBackupAt: string | null;
	lastImportAt: string | null;
}

const DEFAULT_STORAGE_INFO: StorageInfo = {
	migratedFromLocalStorage: false,
	lastBackupAt: null,
	lastImportAt: null,
};

export interface StorageDiagnostics {
	provider: 'indexeddb';
	dbName: string;
	dbVersion: number;
	localOnly: true;
	noteCount: number;
	counter: number;
	migratedFromLocalStorage: boolean;
	lastBackupAt: string | null;
	lastImportAt: string | null;
}

/** Open (or create) the database, upgrading schema as needed. */
async function getDb(): Promise<IDBPDatabase> {
	if (dbInstance) return dbInstance;

	dbInstance = await openDB(DB_NAME, DB_VERSION, {
		upgrade(db) {
			if (!db.objectStoreNames.contains(NOTES_STORE)) {
				db.createObjectStore(NOTES_STORE, { keyPath: 'id' });
			}
			if (!db.objectStoreNames.contains(META_STORE)) {
				db.createObjectStore(META_STORE);
			}
		},
	});

	return dbInstance;
}

function normalizeStorageInfo(value: unknown): StorageInfo {
	if (!value || typeof value !== 'object') {
		return { ...DEFAULT_STORAGE_INFO };
	}

	const input = value as Partial<StorageInfo>;
	return {
		migratedFromLocalStorage: input.migratedFromLocalStorage === true,
		lastBackupAt: typeof input.lastBackupAt === 'string' ? input.lastBackupAt : null,
		lastImportAt: typeof input.lastImportAt === 'string' ? input.lastImportAt : null,
	};
}

async function loadStorageInfo(db: IDBPDatabase): Promise<StorageInfo> {
	const value = await db.get(META_STORE, STORAGE_INFO_KEY);
	return normalizeStorageInfo(value);
}

async function saveStorageInfo(db: IDBPDatabase, info: StorageInfo): Promise<void> {
	await db.put(META_STORE, info, STORAGE_INFO_KEY);
}

async function markStorageEvent(key: 'lastBackupAt' | 'lastImportAt'): Promise<void> {
	const db = await getDb();
	const info = await loadStorageInfo(db);
	await saveStorageInfo(db, { ...info, [key]: new Date().toISOString() });
}

/**
 * Migrate data from localStorage to IndexedDB.
 * Runs once — if localStorage has notes, they are written to IDB
 * and the old keys are removed.
 */
export async function migrateFromLocalStorage(): Promise<void> {
	if (typeof localStorage === 'undefined') return;

	const raw = localStorage.getItem(LS_NOTES_KEY);
	if (!raw) return; // Nothing to migrate

	try {
		const notes: Note[] = JSON.parse(raw);
		if (!Array.isArray(notes) || notes.length === 0) {
			localStorage.removeItem(LS_NOTES_KEY);
			localStorage.removeItem(LS_COUNTER_KEY);
			return;
		}

		const db = await getDb();
		const tx = db.transaction(NOTES_STORE, 'readwrite');
		const store = tx.objectStore(NOTES_STORE);

		for (const note of notes) {
			// Ensure migrated notes have all required fields
			await store.put({
				...note,
				pinned: note.pinned ?? false,
				archived: note.archived ?? false,
				color: note.color ?? 'none',
			});
		}
		await tx.done;

		// Migrate counter
		const counter = parseInt(localStorage.getItem(LS_COUNTER_KEY) ?? '0', 10);
		if (counter > 0) {
			await db.put(META_STORE, counter, 'counter');
		}

		const info = await loadStorageInfo(db);
		await saveStorageInfo(db, {
			...info,
			migratedFromLocalStorage: true,
		});

		// Clean up legacy storage
		localStorage.removeItem(LS_NOTES_KEY);
		localStorage.removeItem(LS_COUNTER_KEY);

		console.log(`[Storage] Migrated ${notes.length} notes from localStorage to IndexedDB`);
	} catch (err) {
		console.error('[Storage] Migration failed:', err);
	}
}

// ─── CRUD Operations ────────────────────────────────────────────

/** Load all notes from IndexedDB. */
export async function loadAll(): Promise<Note[]> {
	const db = await getDb();
	return db.getAll(NOTES_STORE);
}

/** Save (upsert) a single note. */
export async function saveOne(note: Note): Promise<void> {
	const db = await getDb();
	await db.put(NOTES_STORE, note);
}

/** Save all notes (full overwrite). Used during bulk operations like import. */
export async function saveAll(notes: Note[]): Promise<void> {
	const db = await getDb();
	const tx = db.transaction(NOTES_STORE, 'readwrite');
	const store = tx.objectStore(NOTES_STORE);

	// Clear existing and write all
	await store.clear();
	for (const note of notes) {
		await store.put(note);
	}
	await tx.done;
}

/** Remove a note by ID. */
export async function removeOne(id: string): Promise<void> {
	const db = await getDb();
	await db.delete(NOTES_STORE, id);
}

// ─── Counter (meta) ─────────────────────────────────────────────

/** Load the ID counter. */
export async function loadCounter(): Promise<number> {
	const db = await getDb();
	const val = await db.get(META_STORE, 'counter');
	return typeof val === 'number' ? val : 0;
}

/** Save the ID counter. */
export async function saveCounter(value: number): Promise<void> {
	const db = await getDb();
	await db.put(META_STORE, value, 'counter');
}

/** Record that a local backup/export event was triggered. */
export async function recordBackupEvent(): Promise<void> {
	await markStorageEvent('lastBackupAt');
}

/** Record that a local import/restore event was triggered. */
export async function recordImportEvent(): Promise<void> {
	await markStorageEvent('lastImportAt');
}

/** Return current local storage diagnostics for status and operations UI. */
export async function getDiagnostics(): Promise<StorageDiagnostics> {
	const db = await getDb();
	const [noteCount, rawCounter, info] = await Promise.all([
		db.count(NOTES_STORE),
		db.get(META_STORE, 'counter'),
		loadStorageInfo(db),
	]);

	return {
		provider: 'indexeddb',
		dbName: DB_NAME,
		dbVersion: DB_VERSION,
		localOnly: true,
		noteCount,
		counter: typeof rawCounter === 'number' ? rawCounter : 0,
		migratedFromLocalStorage: info.migratedFromLocalStorage,
		lastBackupAt: info.lastBackupAt,
		lastImportAt: info.lastImportAt,
	};
}

/**
 * Reset the cached DB connection. Used by tests to ensure
 * a fresh database between test runs.
 * @internal
 */
export function _resetForTesting(): void {
	if (dbInstance) {
		dbInstance.close();
		dbInstance = null;
	}
}
