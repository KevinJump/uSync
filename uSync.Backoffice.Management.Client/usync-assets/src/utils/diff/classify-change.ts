import type { USyncChange } from '../../api/types.gen.js';
import { USYNC_DIFF_LIMITS, type USyncClassifiedChange } from './types.js';

// `ChangeDetailType` is an enum, and importing it as a value is not permitted
// here (only type-only imports from the generated API types are allowed) -
// so its members are matched against their known string values instead.
const CHANGE_NO_CHANGE = 'NoChange';
const CHANGE_ERROR = 'Error';
const CHANGE_WARNING = 'Warning';

export const USYNC_MASKED_VALUE = '*****';

const RAW_XML_DETAIL_NAME = 'Raw XML';

export type USyncJsonContainerResult = { ok: true; value: unknown } | { ok: false };

/**
 * Parses `text` as a JSON *container* (object or array) only. Bare scalars
 * such as `123` or `"hi"` parse successfully with `JSON.parse` but are never
 * what we mean by "this is a JSON value to structurally diff" - a
 * `textstring` of `123` must not be treated as JSON.
 */
export function tryParseJsonContainer(text: string): USyncJsonContainerResult {
	const trimmed = text.trim();
	if (!(trimmed.startsWith('{') || trimmed.startsWith('['))) return { ok: false };
	try {
		const value: unknown = JSON.parse(trimmed);
		if (value === null || typeof value !== 'object') return { ok: false };
		return { ok: true, value };
	} catch {
		return { ok: false };
	}
}

function byteLength(text: string): number {
	return new TextEncoder().encode(text).length;
}

const XML_START = /^\s*</;

/**
 * Classifies a `USyncChange` detail into a diff strategy, in priority order.
 * See the plan for the full rationale behind the ordering.
 */
export function classifyChange(detail: USyncChange): USyncClassifiedChange {
	const { oldValue, newValue, name } = detail;
	const change: string = detail.change;

	if (change === CHANGE_NO_CHANGE) {
		return skip(oldValue, newValue);
	}

	if (change === CHANGE_ERROR) {
		return notice(oldValue);
	}

	if (change === CHANGE_WARNING) {
		return notice(newValue);
	}

	if (oldValue === USYNC_MASKED_VALUE || newValue === USYNC_MASKED_VALUE) {
		return build('masked', oldValue, newValue, 'text');
	}

	const bytes = Math.max(byteLength(oldValue), byteLength(newValue));
	const forceXml = name === RAW_XML_DETAIL_NAME;

	if (bytes > USYNC_DIFF_LIMITS.maxDiffBytes) {
		return build('oversized', oldValue, newValue, forceXml ? 'xml' : 'text', bytes);
	}

	if (oldValue === newValue) {
		return skip(oldValue, newValue);
	}

	if (oldValue === '' || newValue === '') {
		return build('single', oldValue, newValue, forceXml ? 'xml' : 'text', bytes);
	}

	const oldJson = tryParseJsonContainer(oldValue);
	const newJson = tryParseJsonContainer(newValue);
	if (oldJson.ok && newJson.ok) {
		return {
			strategy: 'json',
			oldText: oldValue,
			newText: newValue,
			oldJson: oldJson.value,
			newJson: newJson.value,
			language: 'json',
			bytes,
		};
	}

	if (forceXml || XML_START.test(oldValue) || XML_START.test(newValue)) {
		return build('lines', oldValue, newValue, 'xml', bytes);
	}

	if (oldValue.includes('\n') || newValue.includes('\n')) {
		return build('lines', oldValue, newValue, 'text', bytes);
	}

	if (bytes <= USYNC_DIFF_LIMITS.inlineScalarChars) {
		return build('scalar', oldValue, newValue, 'text', bytes);
	}

	if (bytes <= USYNC_DIFF_LIMITS.maxWordDiffBytes) {
		return build('words', oldValue, newValue, 'text', bytes);
	}

	return build('lines', oldValue, newValue, 'text', bytes);
}

function skip(oldValue: string, newValue: string): USyncClassifiedChange {
	return build('skip', oldValue, newValue, 'text');
}

function notice(message: string): USyncClassifiedChange {
	return {
		strategy: 'notice',
		oldText: message,
		newText: message,
		language: 'text',
		bytes: byteLength(message),
	};
}

function build(
	strategy: USyncClassifiedChange['strategy'],
	oldValue: string,
	newValue: string,
	language: USyncClassifiedChange['language'],
	bytes?: number,
): USyncClassifiedChange {
	return {
		strategy,
		oldText: oldValue,
		newText: newValue,
		language,
		bytes: bytes ?? Math.max(byteLength(oldValue), byteLength(newValue)),
	};
}
