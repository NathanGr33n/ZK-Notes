import { describe, it, expect } from 'vitest';
import { createNote, NOTE_COLORS } from './types';
import type { Note, NoteType, NoteColor } from './types';

describe('createNote', () => {
	it('creates a note with the given id', () => {
		const note = createNote('ZK-0001');
		expect(note.id).toBe('ZK-0001');
	});

	it('defaults to permanent type', () => {
		const note = createNote('ZK-0001');
		expect(note.type).toBe('permanent');
	});

	it('accepts a custom type', () => {
		const types: NoteType[] = ['fleeting', 'literature', 'permanent', 'standard'];
		for (const type of types) {
			expect(createNote('test', type).type).toBe(type);
		}
	});

	it('starts with empty content, title, tags, links, backlinks', () => {
		const note = createNote('ZK-0001');
		expect(note.title).toBe('');
		expect(note.content).toBe('');
		expect(note.tags).toEqual([]);
		expect(note.links).toEqual([]);
		expect(note.backlinks).toEqual([]);
	});

	it('defaults pinned to false', () => {
		expect(createNote('ZK-0001').pinned).toBe(false);
	});

	it('defaults archived to false', () => {
		expect(createNote('ZK-0001').archived).toBe(false);
	});

	it('defaults color to none', () => {
		expect(createNote('ZK-0001').color).toBe('none');
	});

	it('sets created and lastEdit to the same ISO timestamp', () => {
		const note = createNote('ZK-0001');
		expect(note.created).toBe(note.lastEdit);
		// Verify it's a valid ISO date
		expect(() => new Date(note.created)).not.toThrow();
		expect(new Date(note.created).toISOString()).toBe(note.created);
	});

	it('returns a proper Note shape', () => {
		const note = createNote('ZK-0001', 'fleeting');
		const keys: (keyof Note)[] = [
			'id', 'title', 'content', 'type', 'tags', 'links',
			'backlinks', 'created', 'lastEdit', 'pinned', 'archived', 'color',
		];
		for (const key of keys) {
			expect(note).toHaveProperty(key);
		}
	});
});

describe('NOTE_COLORS', () => {
	it('contains expected color values', () => {
		expect(NOTE_COLORS).toContain('none');
		expect(NOTE_COLORS).toContain('blue');
		expect(NOTE_COLORS).toContain('violet');
		expect(NOTE_COLORS).toContain('teal');
		expect(NOTE_COLORS).toContain('amber');
		expect(NOTE_COLORS).toContain('rose');
		expect(NOTE_COLORS).toContain('sage');
		expect(NOTE_COLORS).toContain('stone');
	});

	it('has 8 colors', () => {
		expect(NOTE_COLORS).toHaveLength(8);
	});
});
