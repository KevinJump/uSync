export interface USyncChangeLabel {
	label: string;
	prefix?: string;
	culture?: string;
	raw: string;
}

// Tight language-tag pattern so a plain "(2)" (e.g. "Sort Order (2)") is never
// mistaken for a culture - a culture must start with 2-3 letters.
const CULTURE_SUFFIX = /^(.*?)\s*\(([A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*)\)\s*$/;

const PROPERTY_PREFIX = /^Property\s*-\s*/;

/**
 * Splits a `USyncChange.name` into a display label, an optional `Property -`
 * prefix, and an optional trailing culture. When nothing matches, the name is
 * left completely alone - other trackers produce arbitrary names and we must
 * not mangle them.
 */
export function parseChangeName(name: string): USyncChangeLabel {
	const cultureMatch = CULTURE_SUFFIX.exec(name);

	let remaining = name;
	let culture: string | undefined;

	if (cultureMatch) {
		remaining = cultureMatch[1];
		culture = cultureMatch[2];
	}

	const prefixMatch = PROPERTY_PREFIX.exec(remaining);
	let prefix: string | undefined;
	let label = remaining;

	if (prefixMatch) {
		prefix = 'Property';
		label = remaining.slice(prefixMatch[0].length);
	}

	if (!cultureMatch && !prefixMatch) {
		return { label: name, raw: name };
	}

	return { label, prefix, culture, raw: name };
}
