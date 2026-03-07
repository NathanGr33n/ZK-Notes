import { describe, it, expect } from 'vitest';
import { extractTags } from './tagParser';

describe('extractTags', () => {
	it('returns empty array for empty/null content', () => {
		expect(extractTags('')).toEqual([]);
		expect(extractTags(null as unknown as string)).toEqual([]);
	});

	it('extracts a single tag', () => {
		expect(extractTags('Hello #world')).toEqual(['world']);
	});

	it('extracts multiple unique tags', () => {
		const tags = extractTags('#alpha and #beta and #gamma');
		expect(tags).toEqual(expect.arrayContaining(['alpha', 'beta', 'gamma']));
		expect(tags).toHaveLength(3);
	});

	it('deduplicates tags (case-insensitive)', () => {
		const tags = extractTags('#Foo and #foo and #FOO');
		expect(tags).toEqual(['foo']);
	});

	it('lowercases all tags', () => {
		const tags = extractTags('#CamelCase #UPPER');
		expect(tags).toEqual(expect.arrayContaining(['camelcase', 'upper']));
	});

	it('supports hyphens in tag names', () => {
		expect(extractTags('#my-tag')).toEqual(['my-tag']);
	});

	it('supports underscores in tag names', () => {
		expect(extractTags('#my_tag')).toEqual(['my_tag']);
	});

	it('supports digits after first letter', () => {
		expect(extractTags('#tag123')).toEqual(['tag123']);
	});

	it('ignores tags inside fenced code blocks', () => {
		const content = '```\n#insideCode\n```\n#outside';
		const tags = extractTags(content);
		expect(tags).toEqual(['outside']);
		expect(tags).not.toContain('insidecode');
	});

	it('ignores tags inside inline code', () => {
		const content = 'Use `#notATag` but #realTag here';
		const tags = extractTags(content);
		expect(tags).toEqual(['realtag']);
	});

	it('requires tag to start with a letter', () => {
		// #123 should not match since tag must start with a letter
		expect(extractTags('#123')).toEqual([]);
	});

	it('extracts tag at the beginning of a line', () => {
		expect(extractTags('#philosophy')).toEqual(['philosophy']);
	});

	it('extracts tags preceded by whitespace', () => {
		expect(extractTags('Note about #topic here')).toEqual(['topic']);
	});

	it('handles multiple code blocks correctly', () => {
		const content = '```js\n#code1\n```\nSome text #real\n```py\n#code2\n```';
		const tags = extractTags(content);
		expect(tags).toEqual(['real']);
	});
});
