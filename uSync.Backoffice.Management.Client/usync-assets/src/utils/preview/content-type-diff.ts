import { deepEqual } from '../diff/deep-equal.js';
import {
	compositionIds,
	type USyncPreviewContainer,
	type USyncPreviewContentType,
	type USyncPreviewProperty,
} from './content-type-layout.js';

export type USyncPreviewStatus = 'added' | 'removed' | 'changed';

export interface USyncPreviewItemChange {
	status: USyncPreviewStatus;
	/** the fields that differ (for a changed item) */
	fields: string[];
}

export interface USyncContentTypeDiff {
	/** top level fields (name, icon, compositions...) that differ */
	fields: Set<string>;
	properties: Map<string, USyncPreviewItemChange>;
	containers: Map<string, USyncPreviewItemChange>;
}

const TYPE_FIELDS = [
	'alias',
	'name',
	'description',
	'icon',
	'allowedAsRoot',
	'variesByCulture',
	'variesBySegment',
	'isElement',
	'collection',
] as const;

const PROPERTY_FIELDS = [
	'alias',
	'name',
	'description',
	'dataType',
	'container',
	'sortOrder',
	'variesByCulture',
	'variesBySegment',
	'validation',
	'appearance',
] as const;

const CONTAINER_FIELDS = ['name', 'type', 'parent', 'sortOrder'] as const;

/**
 * compare two versions of a content type by key. Items only in the current
 * version are being removed, items only in the incoming version are being added.
 */
export function diffContentTypes(
	current: USyncPreviewContentType | undefined,
	incoming: USyncPreviewContentType | undefined,
): USyncContentTypeDiff {
	const fields = new Set<string>();

	if (current && incoming) {
		for (const field of TYPE_FIELDS) {
			if (!deepEqual(current[field], incoming[field])) fields.add(field);
		}
		if (
			!deepEqual(
				[...compositionIds(current)].sort(),
				[...compositionIds(incoming)].sort(),
			)
		) {
			fields.add('compositions');
		}
	}

	return {
		fields,
		properties: diffById<USyncPreviewProperty>(
			current?.properties,
			incoming?.properties,
			PROPERTY_FIELDS,
		),
		containers: diffById<USyncPreviewContainer>(
			current?.containers,
			incoming?.containers,
			CONTAINER_FIELDS,
		),
	};
}

function diffById<T extends { id: string }>(
	current: T[] | undefined,
	incoming: T[] | undefined,
	fields: readonly (keyof T)[],
): Map<string, USyncPreviewItemChange> {
	const changes = new Map<string, USyncPreviewItemChange>();

	// a whole type being added or removed is shown by its headline, don't mark every item.
	if (!current || !incoming) return changes;

	const before = new Map(current.map((item) => [item.id, item]));
	const after = new Map(incoming.map((item) => [item.id, item]));

	for (const [id, item] of before) {
		const other = after.get(id);
		if (!other) {
			changes.set(id, { status: 'removed', fields: [] });
			continue;
		}
		const changed = fields
			.filter((field) => !deepEqual(item[field], other[field]))
			.map(String);
		if (changed.length > 0) changes.set(id, { status: 'changed', fields: changed });
	}

	for (const id of after.keys()) {
		if (!before.has(id)) changes.set(id, { status: 'added', fields: [] });
	}

	return changes;
}
