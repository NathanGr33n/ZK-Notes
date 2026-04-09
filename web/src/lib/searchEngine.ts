/**
 * Fuzzy search engine for Zettelkasten notes.
 *
 * Supports four matching strategies (tried in order of quality):
 *   1. Exact substring  — highest score
 *   2. Token matching    — multi-word queries, each word matched independently
 *   3. Subsequence       — characters appear in order (handles abbreviations)
 *   4. Bigram similarity — typo tolerance for short queries
 *
 * Results are ranked by field weight (title > id > tags > content)
 * and include highlight indices for rendering <mark> tags.
 */

import type { Note } from './types';

// ─── Public Types ───────────────────────────────────────────────

/** A single match against one field of a note. */
export interface SearchMatch {
	field: 'title' | 'content' | 'tags' | 'id';
	/** [start, end] index pairs into the matched string (inclusive). */
	indices: [number, number][];
	/** The raw field value that was matched against. */
	value: string;
}

/** A ranked search result for one note. */
export interface SearchResult {
	noteId: string;
	score: number;
	matches: SearchMatch[];
}

// ─── Configuration ──────────────────────────────────────────────

/** Weight multipliers per field — higher means the field ranks more. */
const FIELD_WEIGHTS: Record<string, number> = {
	title: 10,
	id: 6,
	tags: 5,
	content: 2,
};

const EXACT_BONUS = 3;
const PREFIX_BONUS = 2;
const WORD_BOUNDARY_BONUS = 1.5;
const BIGRAM_THRESHOLD = 0.4;

// ─── Core Fuzzy Match ───────────────────────────────────────────

interface FuzzyResult {
	score: number;
	indices: [number, number][];
}

/**
 * Attempt to fuzzy-match `query` against `target`.
 * Returns a score + highlight indices, or null if no match.
 */
export function fuzzyMatch(query: string, target: string): FuzzyResult | null {
	if (!query || !target) return null;

	const q = query.toLowerCase();
	const t = target.toLowerCase();

	// 1. Exact substring
	const exactIdx = t.indexOf(q);
	if (exactIdx !== -1) {
		let score = 1.0;
		if (exactIdx === 0) score *= PREFIX_BONUS;
		if (q.length === t.length) score *= EXACT_BONUS;
		if (exactIdx === 0 || /\W/.test(t[exactIdx - 1])) score *= WORD_BOUNDARY_BONUS;

		return { score, indices: [[exactIdx, exactIdx + q.length - 1]] };
	}

	// 2. Token matching (multi-word queries)
	const queryTokens = q.split(/\s+/).filter(Boolean);
	if (queryTokens.length > 1) {
		const tokenResult = matchTokens(queryTokens, t, q.length);
		if (tokenResult) return tokenResult;
	}

	// 3. Subsequence matching
	const subResult = subsequenceMatch(q, t);
	if (subResult) {
		const firstStart = subResult.indices[0][0];
		const lastEnd = subResult.indices[subResult.indices.length - 1][1];
		const span = lastEnd - firstStart + 1;
		const density = q.length / span;
		return { score: density * 0.5, indices: subResult.indices };
	}

	// 4. Bigram similarity (typo tolerance, short queries only)
	if (q.length >= 3 && q.length <= 20) {
		const biResult = bigramMatch(q, t);
		if (biResult) return biResult;
	}

	return null;
}

// ─── Strategy Helpers ───────────────────────────────────────────

function matchTokens(tokens: string[], target: string, queryLen: number): FuzzyResult | null {
	const allIndices: [number, number][] = [];
	let matchedTokens = 0;
	let totalScore = 0;

	for (const token of tokens) {
		const idx = target.indexOf(token);
		if (idx !== -1) {
			allIndices.push([idx, idx + token.length - 1]);
			matchedTokens++;
			totalScore += token.length / queryLen;
		}
	}

	if (matchedTokens === 0) return null;

	const ratio = matchedTokens / tokens.length;
	return {
		score: totalScore * ratio * 0.8,
		indices: mergeIndices(allIndices),
	};
}

/**
 * Characters of `query` appear in order within `target`.
 * Tracks contiguous runs for tight highlight ranges.
 */
function subsequenceMatch(query: string, target: string): { indices: [number, number][] } | null {
	const indices: [number, number][] = [];
	let qi = 0;
	let runStart = -1;

	for (let ti = 0; ti < target.length && qi < query.length; ti++) {
		if (target[ti] === query[qi]) {
			if (runStart === -1) runStart = ti;
			qi++;
			// End run if next pair doesn't match
			if (qi >= query.length || target[ti + 1] !== query[qi]) {
				indices.push([runStart, ti]);
				runStart = -1;
			}
		} else if (runStart !== -1) {
			indices.push([runStart, ti - 1]);
			runStart = -1;
		}
	}

	return qi >= query.length ? { indices } : null;
}

function bigramMatch(query: string, target: string): FuzzyResult | null {
	const words = target.split(/\s+/);
	let bestSim = 0;
	let bestWordStart = 0;
	let bestWord = '';
	let pos = 0;

	for (const word of words) {
		const start = target.indexOf(word, pos);
		const sim = bigramSimilarity(query, word);
		if (sim > bestSim) {
			bestSim = sim;
			bestWordStart = start;
			bestWord = word;
		}
		pos = start + word.length;
	}

	if (bestSim < BIGRAM_THRESHOLD) return null;

	return {
		score: bestSim * 0.3,
		indices: [[bestWordStart, bestWordStart + bestWord.length - 1]],
	};
}

/** Dice coefficient over character bigrams. Returns 0–1. */
export function bigramSimilarity(a: string, b: string): number {
	if (a.length < 2 || b.length < 2) return 0;

	const bigramsA = new Set<string>();
	const bigramsB = new Set<string>();
	for (let i = 0; i < a.length - 1; i++) bigramsA.add(a.slice(i, i + 2));
	for (let i = 0; i < b.length - 1; i++) bigramsB.add(b.slice(i, i + 2));

	let intersection = 0;
	for (const bg of bigramsA) {
		if (bigramsB.has(bg)) intersection++;
	}

	return (2 * intersection) / (bigramsA.size + bigramsB.size);
}

// ─── Index Utilities ────────────────────────────────────────────

/** Merge overlapping or adjacent [start, end] ranges. */
export function mergeIndices(indices: [number, number][]): [number, number][] {
	if (indices.length <= 1) return indices;
	const sorted = [...indices].sort((a, b) => a[0] - b[0]);
	const merged: [number, number][] = [sorted[0]];

	for (let i = 1; i < sorted.length; i++) {
		const prev = merged[merged.length - 1];
		if (sorted[i][0] <= prev[1] + 1) {
			prev[1] = Math.max(prev[1], sorted[i][1]);
		} else {
			merged.push(sorted[i]);
		}
	}
	return merged;
}

// ─── Main Search ────────────────────────────────────────────────

/**
 * Search notes with fuzzy matching and relevance ranking.
 *
 * @param notes  — The full list of notes to search through.
 * @param query  — The user's search query.
 * @returns Ranked array of SearchResult (highest score first).
 */
export function searchNotes(notes: Note[], query: string): SearchResult[] {
	const q = query.trim();
	if (!q) return [];

	const results: SearchResult[] = [];

	for (const note of notes) {
		const matches: SearchMatch[] = [];
		let totalScore = 0;

		// Search title, id, content
		for (const field of ['title', 'id', 'content'] as const) {
			const value = note[field];
			if (!value) continue;
			const result = fuzzyMatch(q, value);
			if (result) {
				matches.push({ field, indices: result.indices, value });
				totalScore += result.score * FIELD_WEIGHTS[field];
			}
		}

		// Search tags (match against each, take best)
		let bestTagScore = 0;
		let bestTagMatch: SearchMatch | null = null;
		for (const tag of note.tags) {
			const result = fuzzyMatch(q, tag);
			if (result && result.score > bestTagScore) {
				bestTagScore = result.score;
				bestTagMatch = { field: 'tags', indices: result.indices, value: tag };
			}
		}
		if (bestTagMatch) {
			matches.push(bestTagMatch);
			totalScore += bestTagScore * FIELD_WEIGHTS.tags;
		}

		if (matches.length > 0) {
			results.push({ noteId: note.id, score: totalScore, matches });
		}
	}

	return results.sort((a, b) => b.score - a.score);
}

// ─── Highlight Rendering ────────────────────────────────────────

/**
 * Render a string with `<mark>` tags wrapping the matched ranges.
 * Safely escapes HTML in the surrounding text.
 */
export function highlightMatch(text: string, indices: [number, number][]): string {
	if (!indices.length) return escapeHtml(text);

	const sorted = [...indices].sort((a, b) => a[0] - b[0]);
	let result = '';
	let cursor = 0;

	for (const [start, end] of sorted) {
		if (start > cursor) result += escapeHtml(text.slice(cursor, start));
		result += `<mark>${escapeHtml(text.slice(start, end + 1))}</mark>`;
		cursor = end + 1;
	}

	if (cursor < text.length) result += escapeHtml(text.slice(cursor));
	return result;
}

function escapeHtml(str: string): string {
	return str
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;');
}
