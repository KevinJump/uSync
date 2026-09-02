/**
 * A short tag describing the "shape" of a value, used to decide whether two
 * values are even comparable as the same kind of thing (object vs array vs
 * primitive) before doing a structural walk.
 */
export function typeTag(value: unknown): string {
	if (value === null) return 'null';
	if (Array.isArray(value)) return 'array';
	const t = typeof value;
	if (t === 'object') return 'object';
	return t;
}

export function isPlainObject(value: unknown): value is Record<string, unknown> {
	if (value === null || typeof value !== 'object') return false;
	if (Array.isArray(value)) return false;
	const proto = Object.getPrototypeOf(value);
	return proto === Object.prototype || proto === null;
}

/**
 * Structural equality for JSON-shaped values (objects, arrays, strings,
 * numbers, booleans, null). Array order is significant. Object key order is
 * not.
 */
export function deepEqual(a: unknown, b: unknown): boolean {
	if (a === b) return true;

	if (Array.isArray(a) && Array.isArray(b)) {
		if (a.length !== b.length) return false;
		for (let i = 0; i < a.length; i++) {
			if (!deepEqual(a[i], b[i])) return false;
		}
		return true;
	}

	if (isPlainObject(a) && isPlainObject(b)) {
		const aKeys = Object.keys(a);
		const bKeys = Object.keys(b);
		if (aKeys.length !== bKeys.length) return false;
		for (const key of aKeys) {
			if (!Object.prototype.hasOwnProperty.call(b, key)) return false;
			if (!deepEqual(a[key], b[key])) return false;
		}
		return true;
	}

	// Different shapes (one array one object, one object one primitive, etc.)
	// or unequal primitives - already excluded by the `a === b` check above.
	return false;
}
