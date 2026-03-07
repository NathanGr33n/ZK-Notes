import { describe, it, expect } from 'vitest';
import { extractLinks, extractLinkTargets, replaceLinksWithHtml } from './linkParser';

describe('extractLinks', () => {
	it('returns empty array for empty/null content', () => {
		expect(extractLinks('')).toEqual([]);
		expect(extractLinks(null as unknown as string)).toEqual([]);
	});

	it('extracts a single wiki link', () => {
		const result = extractLinks('See [[ZK-0001]] for details.');
		expect(result).toHaveLength(1);
		expect(result[0]).toEqual({
			target: 'ZK-0001',
			display: 'ZK-0001',
			raw: '[[ZK-0001]]',
		});
	});

	it('extracts multiple links', () => {
		const result = extractLinks('Link [[A]] and [[B]] and [[C]].');
		expect(result).toHaveLength(3);
		expect(result.map((l) => l.target)).toEqual(['A', 'B', 'C']);
	});

	it('parses aliased links with pipe syntax', () => {
		const result = extractLinks('See [[ZK-0001|my note]] for details.');
		expect(result).toHaveLength(1);
		expect(result[0].target).toBe('ZK-0001');
		expect(result[0].display).toBe('my note');
	});

	it('trims whitespace inside brackets', () => {
		const result = extractLinks('See [[ ZK-0001 | my note ]] here.');
		expect(result[0].target).toBe('ZK-0001');
		expect(result[0].display).toBe('my note');
	});

	it('skips empty bracket pairs', () => {
		const result = extractLinks('Empty [[  ]] link.');
		expect(result).toHaveLength(0);
	});

	it('handles links with special characters in title', () => {
		const result = extractLinks('See [[My Note: A Study (2024)]] here.');
		expect(result[0].target).toBe('My Note: A Study (2024)');
	});
});

describe('extractLinkTargets', () => {
	it('returns target strings only', () => {
		const targets = extractLinkTargets('Link [[A|Display]] and [[B]].');
		expect(targets).toEqual(['A', 'B']);
	});

	it('returns empty array for no links', () => {
		expect(extractLinkTargets('No links here.')).toEqual([]);
	});
});

describe('replaceLinksWithHtml', () => {
	it('returns empty string for empty content', () => {
		expect(replaceLinksWithHtml('')).toBe('');
	});

	it('replaces a wiki link with an anchor tag', () => {
		const result = replaceLinksWithHtml('See [[ZK-0001]] here.');
		expect(result).toContain('data-note-link="ZK-0001"');
		expect(result).toContain('>ZK-0001</a>');
		expect(result).not.toContain('[[');
	});

	it('uses display text for aliased links', () => {
		const result = replaceLinksWithHtml('See [[ZK-0001|My Note]] here.');
		expect(result).toContain('data-note-link="ZK-0001"');
		expect(result).toContain('>My Note</a>');
	});

	it('encodes target in data attribute', () => {
		const result = replaceLinksWithHtml('See [[Note With Spaces]] here.');
		expect(result).toContain('data-note-link="Note%20With%20Spaces"');
	});

	it('escapes HTML in display text', () => {
		const result = replaceLinksWithHtml('See [[<script>alert(1)</script>]] here.');
		expect(result).not.toContain('<script>');
		expect(result).toContain('&lt;script&gt;');
	});

	it('preserves non-link content unchanged', () => {
		const result = replaceLinksWithHtml('Hello world.');
		expect(result).toBe('Hello world.');
	});
});
