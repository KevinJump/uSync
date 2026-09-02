import { deepEqual, isPlainObject, typeTag } from './deep-equal.js';
import { elementPath, joinKey } from './format-value.js';
import { tryParseJsonContainer } from './classify-change.js';
import {
	USYNC_DIFF_LIMITS,
	type USyncJsonDiffResult,
	type USyncLeafChange,
} from './types.js';

// A private sentinel for "this side has no value at all" - distinct from
// `undefined`, which can legitimately appear as the result of a string
// expansion round-trip. This keeps `{"a": null}` vs `{}` distinguishable from
// a missing key.
const MISSING = Symbol('usync-diff-missing');
type Missing = typeof MISSING;

/**
 * Composite identities are tried before single fields, and in the order
 * listed - a block's `values` entries and property values key on
 * alias(+culture/segment) almost always, and that composite survives
 * serialisation round-trips even when raw GUIDs don't.
 */
export const USYNC_COMPOSITE_IDENTITIES: string[][] = [
	['alias', 'culture', 'segment'], // block `values`, property values
	['propertyAlias', 'culture', 'segment'],
	['contentKey', 'culture', 'segment'],
];

// `$type` is deliberately excluded - it's a label, not an identity: it's
// never unique within a block array.
export const USYNC_IDENTITY_FIELDS: string[] = [
	'key',
	'udi',
	'contentUdi',
	'contentKey',
	'settingsKey',
	'id',
	'alias',
	'editorAlias',
	'name',
];

interface IdentityStrategy {
	isComposite: boolean;
	keyOf(element: Record<string, unknown>): string;
	labelOf(element: Record<string, unknown>): string;
}

function isUniqueAndIntersecting(
	a: unknown[],
	b: unknown[],
	keyOf: (element: Record<string, unknown>) => string,
): boolean {
	const aKeys = a.map((element) => keyOf(element as Record<string, unknown>));
	const bKeys = b.map((element) => keyOf(element as Record<string, unknown>));
	if (new Set(aKeys).size !== aKeys.length) return false;
	if (new Set(bKeys).size !== bKeys.length) return false;

	// The load-bearing check: Umbraco regenerates block `key` GUIDs on some
	// serialisation paths. If both sides have unique but entirely disjoint
	// keys, identity matching would report "everything removed, everything
	// added" - strictly worse than index matching. Requiring a shared key
	// means we only key when the key actually carries continuity across the
	// two sides; otherwise we correctly fall back to index matching.
	const bSet = new Set(bKeys);
	return aKeys.some((key) => bSet.has(key));
}

function tryCompositeStrategy(
	fields: string[],
	a: unknown[],
	b: unknown[],
): IdentityStrategy | null {
	const primary = fields[0];
	const hasPrimary = (elements: unknown[]) =>
		elements.every((element) => {
			if (!isPlainObject(element)) return false;
			const value = element[primary];
			return typeof value === 'string' && value.length > 0;
		});
	if (!hasPrimary(a) || !hasPrimary(b)) return null;

	const keyOf = (element: Record<string, unknown>) =>
		fields
			.map((field) =>
				typeof element[field] === 'string' ? (element[field] as string) : '',
			)
			.join('\u0000');

	const labelOf = (element: Record<string, unknown>) => {
		const primaryValue = String(element[primary]);
		const culture = fields.includes('culture') ? element.culture : undefined;
		return typeof culture === 'string' && culture.length > 0
			? `${primaryValue} (${culture})`
			: primaryValue;
	};

	if (!isUniqueAndIntersecting(a, b, keyOf)) return null;
	return { isComposite: true, keyOf, labelOf };
}

function trySingleFieldStrategy(
	field: string,
	a: unknown[],
	b: unknown[],
): IdentityStrategy | null {
	const hasField = (elements: unknown[]) =>
		elements.every((element) => {
			if (!isPlainObject(element)) return false;
			const value = element[field];
			return typeof value === 'string' || typeof value === 'number';
		});
	if (!hasField(a) || !hasField(b)) return null;

	const keyOf = (element: Record<string, unknown>) => String(element[field]);
	const labelOf = (element: Record<string, unknown>) => String(element[field]);

	if (!isUniqueAndIntersecting(a, b, keyOf)) return null;
	return { isComposite: false, keyOf, labelOf };
}

/**
 * Picks an identity strategy for matching elements of two arrays by identity
 * rather than position, or `null` when neither array offers one (the caller
 * should fall back to index matching).
 */
export function pickIdentity(a: unknown[], b: unknown[]): IdentityStrategy | null {
	if (a.length === 0 || b.length === 0) return null;
	if (!a.every(isPlainObject) || !b.every(isPlainObject)) return null;

	for (const fields of USYNC_COMPOSITE_IDENTITIES) {
		const strategy = tryCompositeStrategy(fields, a, b);
		if (strategy) return strategy;
	}
	for (const field of USYNC_IDENTITY_FIELDS) {
		const strategy = trySingleFieldStrategy(field, a, b);
		if (strategy) return strategy;
	}
	return null;
}

interface WalkContext {
	changes: USyncLeafChange[];
	totalCount: number;
	reordered: string[];
	maxChanges: number;
	maxDepth: number;
	expandJsonStrings: boolean;
	maxStringExpansions: number;
	stopped: boolean;
}

function record(
	ctx: WalkContext,
	expansions: number,
	entry: Omit<USyncLeafChange, 'expanded'>,
): void {
	ctx.totalCount++;
	if (ctx.changes.length < ctx.maxChanges) {
		// Any leaf recorded while inside an expanded encoded-JSON string
		// carries `expanded: true`, not just the leaf that triggered the
		// expansion - it's the whole subtree that crossed into a string.
		ctx.changes.push(expansions > 0 ? { ...entry, expanded: true } : entry);
	}
	// Hard-stop: don't keep walking a wholesale change (e.g. a 5000-element
	// array rewrite) just to increment a counter past a few times the cap.
	if (ctx.totalCount > ctx.maxChanges * 4) {
		ctx.stopped = true;
	}
}

function identityChildPath(
	path: string,
	strategy: IdentityStrategy,
	element: Record<string, unknown>,
	index: number,
): string {
	return strategy.isComposite
		? elementPath(path, element, { identityLabel: strategy.labelOf(element) })
		: elementPath(path, element, { index });
}

function walk(
	ctx: WalkContext,
	path: string,
	a: unknown | Missing,
	b: unknown | Missing,
	depth: number,
	expansions: number,
): void {
	if (ctx.stopped) return;

	if (a === MISSING) {
		record(ctx, expansions, { path, kind: 'added', newValue: b });
		return;
	}
	if (b === MISSING) {
		record(ctx, expansions, { path, kind: 'removed', oldValue: a });
		return;
	}
	if (deepEqual(a, b)) return;

	if (depth >= ctx.maxDepth) {
		record(ctx, expansions, { path, kind: 'changed', oldValue: a, newValue: b });
		return;
	}

	if (typeof a === 'string' && typeof b === 'string') {
		if (ctx.expandJsonStrings && expansions < ctx.maxStringExpansions) {
			const ea = tryParseJsonContainer(a);
			const eb = tryParseJsonContainer(b);
			// Only expand when BOTH sides parse - expanding one side only
			// would compare a JSON structure against a raw string.
			if (ea.ok && eb.ok) {
				walk(ctx, `${path} » `, ea.value, eb.value, depth + 1, expansions + 1);
				return;
			}
		}
		record(ctx, expansions, { path, kind: 'changed', oldValue: a, newValue: b });
		return;
	}

	if (typeTag(a) !== typeTag(b)) {
		record(ctx, expansions, { path, kind: 'changed', oldValue: a, newValue: b });
		return;
	}

	if (Array.isArray(a) && Array.isArray(b)) {
		walkArray(ctx, path, a, b, depth, expansions);
		return;
	}

	if (isPlainObject(a) && isPlainObject(b)) {
		walkObject(ctx, path, a, b, depth, expansions);
		return;
	}

	record(ctx, expansions, { path, kind: 'changed', oldValue: a, newValue: b });
}

function walkObject(
	ctx: WalkContext,
	path: string,
	a: Record<string, unknown>,
	b: Record<string, unknown>,
	depth: number,
	expansions: number,
): void {
	for (const key of Object.keys(a)) {
		if (ctx.stopped) return;
		const childPath = joinKey(path, key);
		const bHasKey = Object.prototype.hasOwnProperty.call(b, key);
		walk(ctx, childPath, a[key], bHasKey ? b[key] : MISSING, depth + 1, expansions);
	}
	for (const key of Object.keys(b)) {
		if (ctx.stopped) return;
		if (Object.prototype.hasOwnProperty.call(a, key)) continue;
		walk(ctx, joinKey(path, key), MISSING, b[key], depth + 1, expansions);
	}
}

function sameMultiset(a: string[], b: string[]): boolean {
	if (a.length !== b.length) return false;
	const sorted = (arr: string[]) => [...arr].sort();
	const sa = sorted(a);
	const sb = sorted(b);
	return sa.every((key, i) => key === sb[i]);
}

function sameOrder(a: string[], b: string[]): boolean {
	return a.length === b.length && a.every((key, i) => key === b[i]);
}

function walkArray(
	ctx: WalkContext,
	path: string,
	a: unknown[],
	b: unknown[],
	depth: number,
	expansions: number,
): void {
	const strategy = pickIdentity(a, b);
	if (!strategy) {
		walkArrayByIndex(ctx, path, a, b, depth, expansions);
		return;
	}

	const aEntries = a.map((element, index) => ({
		element: element as Record<string, unknown>,
		index,
		key: strategy.keyOf(element as Record<string, unknown>),
	}));
	const bEntries = b.map((element, index) => ({
		element: element as Record<string, unknown>,
		index,
		key: strategy.keyOf(element as Record<string, unknown>),
	}));
	const bByKey = new Map(bEntries.map((entry) => [entry.key, entry]));
	const aByKey = new Map(aEntries.map((entry) => [entry.key, entry]));

	for (const aEntry of aEntries) {
		if (ctx.stopped) return;
		const bEntry = bByKey.get(aEntry.key);
		if (!bEntry) {
			record(ctx, expansions, {
				path: identityChildPath(path, strategy, aEntry.element, aEntry.index),
				kind: 'removed',
				oldValue: aEntry.element,
			});
			continue;
		}
		walk(
			ctx,
			identityChildPath(path, strategy, bEntry.element, bEntry.index),
			aEntry.element,
			bEntry.element,
			depth + 1,
			expansions,
		);
	}
	for (const bEntry of bEntries) {
		if (ctx.stopped) return;
		if (!aByKey.has(bEntry.key)) {
			record(ctx, expansions, {
				path: identityChildPath(path, strategy, bEntry.element, bEntry.index),
				kind: 'added',
				newValue: bEntry.element,
			});
		}
	}

	const aKeys = aEntries.map((entry) => entry.key);
	const bKeys = bEntries.map((entry) => entry.key);
	if (sameMultiset(aKeys, bKeys) && !sameOrder(aKeys, bKeys)) {
		ctx.reordered.push(path.length > 0 ? path : '(root)');
	}
}

function walkArrayByIndex(
	ctx: WalkContext,
	path: string,
	a: unknown[],
	b: unknown[],
	depth: number,
	expansions: number,
): void {
	const minLen = Math.min(a.length, b.length);

	let start = 0;
	while (start < minLen && deepEqual(a[start], b[start])) start++;

	let end = 0;
	while (
		end < minLen - start &&
		deepEqual(a[a.length - 1 - end], b[b.length - 1 - end])
	) {
		end++;
	}

	const aMid = a.slice(start, a.length - end);
	const bMid = b.slice(start, b.length - end);
	const midLen = Math.max(aMid.length, bMid.length);

	for (let i = 0; i < midLen; i++) {
		if (ctx.stopped) return;
		const aEl: unknown | Missing = i < aMid.length ? aMid[i] : MISSING;
		const bEl: unknown | Missing = i < bMid.length ? bMid[i] : MISSING;
		const index = start + i;
		const sample = aEl !== MISSING ? aEl : bEl;
		const childPath = elementPath(path, sample, { index });
		walk(ctx, childPath, aEl, bEl, depth + 1, expansions);
	}
}

export interface USyncJsonDiffOptions {
	maxChanges?: number;
	maxDepth?: number;
	expandJsonStrings?: boolean;
	maxStringExpansions?: number;
}

/**
 * Structurally diffs two parsed JSON values, producing a flat list of leaf
 * changes with readable paths instead of a wholesale before/after dump.
 * Arrays are matched by identity where possible (see `pickIdentity`) so that
 * inserting one block does not make every other block look changed.
 */
export function diffJsonValues(
	oldValue: unknown,
	newValue: unknown,
	options?: USyncJsonDiffOptions,
): USyncJsonDiffResult {
	const ctx: WalkContext = {
		changes: [],
		totalCount: 0,
		reordered: [],
		maxChanges: options?.maxChanges ?? USYNC_DIFF_LIMITS.maxLeafChanges,
		maxDepth: options?.maxDepth ?? USYNC_DIFF_LIMITS.maxJsonDepth,
		expandJsonStrings: options?.expandJsonStrings ?? true,
		maxStringExpansions:
			options?.maxStringExpansions ?? USYNC_DIFF_LIMITS.maxStringExpansions,
		stopped: false,
	};

	walk(ctx, '', oldValue, newValue, 0, 0);

	return {
		changes: ctx.changes,
		totalCount: ctx.totalCount,
		truncated: ctx.totalCount > ctx.changes.length,
		stopped: ctx.stopped,
		reordered: ctx.reordered,
	};
}
