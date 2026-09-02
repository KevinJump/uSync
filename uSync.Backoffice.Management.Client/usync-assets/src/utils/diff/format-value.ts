import { isPlainObject } from './deep-equal.js';

const IDENTIFIER_KEY = /^[A-Za-z_$][A-Za-z0-9_$]*$/;

/**
 * Joins a path segment onto a base path, using `.key` for identifier-safe
 * keys and `["odd key"]` otherwise.
 */
export function joinKey(base: string, key: string): string {
	if (IDENTIFIER_KEY.test(key)) {
		return base.length > 0 ? `${base}.${key}` : key;
	}
	return `${base}[${JSON.stringify(key)}]`;
}

const ELEMENT_LABEL_FIELDS = [
	'name',
	'alias',
	'editorAlias',
	'propertyAlias',
	'contentTypeAlias',
	'$type',
	'culture',
];

function deriveElementLabel(element: unknown): string | undefined {
	if (!isPlainObject(element)) return undefined;
	for (const field of ELEMENT_LABEL_FIELDS) {
		const value = element[field];
		if (typeof value === 'string' && value.length > 0) return value;
	}
	return undefined;
}

export interface USyncElementPathOptions {
	/** Positional index, used when no identity label is available. */
	index?: number;
	/**
	 * A pre-built identity label (e.g. `headline (en-US)` for the
	 * alias+culture composite identity) - when set this *is* the element's
	 * identity, so no index/derived-label lookup happens.
	 */
	identityLabel?: string;
}

/**
 * Builds a readable path segment for an array element, e.g. `[0: Hero]` for
 * an index-matched element, or `[headline (en-US)]` for an element matched by
 * a composite alias+culture identity.
 */
export function elementPath(
	base: string,
	element: unknown,
	options: USyncElementPathOptions = {},
): string {
	if (options.identityLabel !== undefined) {
		return `${base}[${options.identityLabel}]`;
	}
	const index = options.index ?? 0;
	const label = deriveElementLabel(element);
	return label !== undefined ? `${base}[${index}: ${label}]` : `${base}[${index}]`;
}

const LEAF_SEGMENT = /(\.[^.[\]]+|\[[^\]]*\])$/;

/**
 * Truncates a long path in the middle, keeping the trailing leaf segment
 * (the part the reader actually cares about) fully intact.
 */
export function formatDiffPath(path: string, maxLen = 120): string {
	if (path.length <= maxLen) return path;
	const leafMatch = LEAF_SEGMENT.exec(path);
	const leaf = leafMatch ? leafMatch[0] : '';
	const headLen = Math.max(maxLen - leaf.length - 1, 0);
	const head = path.slice(0, headLen);
	return `${head}…${leaf}`;
}

/**
 * Short, display-agnostic stringification of a leaf value for inline
 * rendering (e.g. `old → new`). Callers decide layout/markup; this only
 * turns a value into text.
 */
export function formatLeafValue(value: unknown): string {
	if (value === undefined) return 'undefined';
	if (value === null) return 'null';
	if (typeof value === 'string') return value;
	if (typeof value === 'number' || typeof value === 'boolean') return String(value);
	return JSON.stringify(value);
}

/** Pretty-prints a parsed JSON value the same way the server does. */
export function prettyJson(value: unknown): string {
	return JSON.stringify(value, null, 1);
}

/** Truncates a string in the middle to roughly `maxLen` characters. */
export function truncateMiddle(text: string, maxLen: number): string {
	if (text.length <= maxLen) return text;
	const ellipsis = '…';
	const keep = Math.max(maxLen - ellipsis.length, 0);
	const headLen = Math.ceil(keep / 2);
	const tailLen = Math.floor(keep / 2);
	return `${text.slice(0, headLen)}${ellipsis}${text.slice(text.length - tailLen)}`;
}
