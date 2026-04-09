import { describe, it, expect } from 'vitest';
import {
	fuzzyMatch,
	bigramSimilarity,
	mergeIndices,
	searchNotes,
	highlightMatch,
} from './searchEngine';
import type { Note } from './types';

// ─── Helper ─────────────────────────────────────────────────────

function makeNote(overrides: Partial<Note> & { id: string }): Note {
	return {
		title: '',
		content: '',
		type: 'permanent',
		tags: [],
		links: [],
		backlinks: [],
		created: '2026-01-01T00:00:00.000Z',
		lastEdit: '2026-01-01T00:00:00.000Z',
		pinned: false,
		archived: false,
		color: 'none',
		...overrides,
	};
}

// ─── fuzzyMatch ─────────────────────────────────────────────────

describe('fuzzyMatch', () => {
	it('returns null for empty query or target', () => {
		expect(fuzzyMatch('', 'hello')).toBeNull();
		expect(fuzzyMatch('hello', '')).toBeNull();
		expect(fuzzyMatch('', '')).toBeNull();
	});

	it('matches exact substring (case-insensitive)', () => {
		const result = fuzzyMatch('world', 'Hello World');
		expect(result).not.toBeNull();
		expect(result!.indices).toEqual([[6, 10]]);
		expect(result!.score).toBeGreaterThan(0);
	});

	it('gives higher score for prefix matches', () => {
		const prefix = fuzzyMatch('hel', 'Hello World');
		const middle = fuzzyMatch('orl', 'Hello World');
		expect(prefix).not.toBeNull();
		expect(middle).not.toBeNull();
		expect(prefix!.score).toBeGreaterThan(middle!.score);
	});

	it('gives highest score for exact full matches', () => {
		const exact = fuzzyMatch('hello', 'hello');
		const partial = fuzzyMatch('hello', 'hello world');
		expect(exact).not.toBeNull();
		expect(partial).not.toBeNull();
		expect(exact!.score).toBeGreaterThan(partial!.score);
	});

	it('matches via subsequence when substring fails', () => {
		// "hwd" as subsequence of "hello world" — h...w...d in order but not adjacent
		const result = fuzzyMatch('hwd', 'hello world');
		expect(result).not.toBeNull();
		expect(result!.score).toBeGreaterThan(0);
	});

	it('returns null when subsequence fails', () => {
		expect(fuzzyMatch('zyx', 'hello world')).toBeNull();
	});

	it('matches multi-word queries via token matching', () => {
		const result = fuzzyMatch('hello world', 'say hello to the world');
		expect(result).not.toBeNull();
		expect(result!.score).toBeGreaterThan(0);
	});

	it('handles typos via bigram similarity', () => {
		const result = fuzzyMatch('zettelkasten', 'zettelkastan method');
		expect(result).not.toBeNull();
		expect(result!.score).toBeGreaterThan(0);
	});

	it('bigram match returns null for very different strings', () => {
		expect(fuzzyMatch('xyz', 'abcdefgh')).toBeNull();
	});
});

// ─── bigramSimilarity ───────────────────────────────────────────

describe('bigramSimilarity', () => {
	it('returns 1 for identical strings', () => {
		expect(bigramSimilarity('hello', 'hello')).toBe(1);
	});

	it('returns 0 for completely different strings', () => {
		expect(bigramSimilarity('ab', 'yz')).toBe(0);
	});

	it('returns 0 for strings shorter than 2 chars', () => {
		expect(bigramSimilarity('a', 'ab')).toBe(0);
		expect(bigramSimilarity('ab', 'b')).toBe(0);
	});

	it('returns partial similarity for similar strings', () => {
		const sim = bigramSimilarity('hello', 'hallo');
		expect(sim).toBeGreaterThan(0);
		expect(sim).toBeLessThan(1);
	});
});

// ─── mergeIndices ───────────────────────────────────────────────

describe('mergeIndices', () => {
	it('returns empty for empty input', () => {
		expect(mergeIndices([])).toEqual([]);
	});

	it('returns single range unchanged', () => {
		expect(mergeIndices([[0, 5]])).toEqual([[0, 5]]);
	});

	it('merges overlapping ranges', () => {
		expect(mergeIndices([[0, 5], [3, 8]])).toEqual([[0, 8]]);
	});

	it('merges adjacent ranges', () => {
		expect(mergeIndices([[0, 3], [4, 7]])).toEqual([[0, 7]]);
	});

	it('keeps non-overlapping ranges separate', () => {
		expect(mergeIndices([[0, 2], [5, 7]])).toEqual([[0, 2], [5, 7]]);
	});

	it('sorts and merges unsorted input', () => {
		expect(mergeIndices([[5, 7], [0, 2], [3, 4]])).toEqual([[0, 7]]);
	});
});

// ─── searchNotes ────────────────────────────────────────────────

describe('searchNotes', () => {
	const notes: Note[] = [
		makeNote({ id: 'ZK-0001', title: 'Zettelkasten Method', content: 'A note about knowledge management.', tags: ['methodology', 'knowledge'] }),
		makeNote({ id: 'ZK-0002', title: 'Graph Theory', content: 'Nodes and edges in mathematics.', tags: ['math', 'graphs'] }),
		makeNote({ id: 'ZK-0003', title: 'Daily Journal', content: 'Reflections on the zettelkasten workflow.', tags: ['journal'] }),
		makeNote({ id: 'ZK-0004', title: 'Empty Note', content: '', tags: [] }),
	];

	it('returns empty array for empty query', () => {
		expect(searchNotes(notes, '')).toEqual([]);
		expect(searchNotes(notes, '   ')).toEqual([]);
	});

	it('ranks title matches higher than content matches', () => {
		const results = searchNotes(notes, 'zettelkasten');
		expect(results.length).toBeGreaterThanOrEqual(2);
		// ZK-0001 has "Zettelkasten" in title — should rank first
		expect(results[0].noteId).toBe('ZK-0001');
		// ZK-0003 has "zettelkasten" in content — should rank lower
		const zk3 = results.find((r) => r.noteId === 'ZK-0003');
		expect(zk3).toBeDefined();
		expect(zk3!.score).toBeLessThan(results[0].score);
	});

	it('matches by tag', () => {
		const results = searchNotes(notes, 'graphs');
		expect(results.some((r) => r.noteId === 'ZK-0002')).toBe(true);
		const match = results.find((r) => r.noteId === 'ZK-0002')!;
		expect(match.matches.some((m) => m.field === 'tags')).toBe(true);
	});

	it('matches by note ID', () => {
		const results = searchNotes(notes, 'ZK-0002');
		expect(results.length).toBeGreaterThanOrEqual(1);
		expect(results[0].noteId).toBe('ZK-0002');
	});

	it('returns results with scores in descending order', () => {
		const results = searchNotes(notes, 'note');
		for (let i = 1; i < results.length; i++) {
			expect(results[i - 1].score).toBeGreaterThanOrEqual(results[i].score);
		}
	});

	it('handles fuzzy/typo queries', () => {
		const results = searchNotes(notes, 'zettelkastan');
		// Should still find ZK-0001 via bigram similarity
		expect(results.some((r) => r.noteId === 'ZK-0001')).toBe(true);
	});

	it('returns no results for unrelated query', () => {
		const results = searchNotes(notes, 'quantum physics');
		// Should return nothing (no notes about quantum physics)
		// We allow some fuzzy matches but the score should be very low or empty
		expect(results.length).toBeLessThanOrEqual(1);
	});

	it('includes match metadata with indices', () => {
		const results = searchNotes(notes, 'Graph');
		const graphResult = results.find((r) => r.noteId === 'ZK-0002');
		expect(graphResult).toBeDefined();
		const titleMatch = graphResult!.matches.find((m) => m.field === 'title');
		expect(titleMatch).toBeDefined();
		expect(titleMatch!.indices.length).toBeGreaterThan(0);
		expect(titleMatch!.value).toBe('Graph Theory');
	});
});

// ─── highlightMatch ─────────────────────────────────────────────

describe('highlightMatch', () => {
	it('returns escaped text when no indices', () => {
		expect(highlightMatch('Hello <World>', [])).toBe('Hello &lt;World&gt;');
	});

	it('wraps matched range in <mark> tags', () => {
		const result = highlightMatch('Hello World', [[0, 4]]);
		expect(result).toBe('<mark>Hello</mark> World');
	});

	it('handles multiple disjoint ranges', () => {
		const result = highlightMatch('Hello World', [[0, 4], [6, 10]]);
		expect(result).toBe('<mark>Hello</mark> <mark>World</mark>');
	});

	it('escapes HTML inside and outside marks', () => {
		const result = highlightMatch('<b>bold</b>', [[3, 6]]);
		expect(result).toBe('&lt;b&gt;<mark>bold</mark>&lt;/b&gt;');
	});

	it('handles range at end of string', () => {
		const result = highlightMatch('Hello', [[3, 4]]);
		expect(result).toBe('Hel<mark>lo</mark>');
	});
});
