import type {
	DocumentTypeResponseModel,
	MediaTypeResponseModel,
	MemberTypeResponseModel,
} from '@umbraco-cms/backoffice/external/backend-api';

/**
 * the shape document, media and member types share - enough to lay out
 * the editor view and compare two versions.
 */
export type USyncPreviewContentType =
	DocumentTypeResponseModel | MediaTypeResponseModel | MemberTypeResponseModel;

export type USyncPreviewProperty = USyncPreviewContentType['properties'][number];
export type USyncPreviewContainer = USyncPreviewContentType['containers'][number];

export interface USyncLayoutProperty {
	property: USyncPreviewProperty;
	/** name of the composition this property comes from (undefined when it's the type's own) */
	inheritedFrom?: string;
}

export interface USyncLayoutGroup {
	name: string;
	/** ids of every container merged into this group (own + compositions) */
	ids: string[];
	inherited: boolean;
	sortOrder: number;
	properties: USyncLayoutProperty[];
}

export interface USyncLayoutTab {
	name: string;
	ids: string[];
	inherited: boolean;
	sortOrder: number;
	/** properties placed directly on the tab, outside any group */
	properties: USyncLayoutProperty[];
	groups: USyncLayoutGroup[];
}

/** name used for the tab that holds groups which aren't inside a tab */
export const USYNC_GENERIC_TAB = '';

/** the ids of the types this one is composed of (or inherits from). */
export function compositionIds(type: USyncPreviewContentType): string[] {
	return type.compositions.map((composition) => {
		if ('documentType' in composition) return composition.documentType.id;
		if ('mediaType' in composition) return composition.mediaType.id;
		return composition.memberType.id;
	});
}

/**
 * build the tabs and groups an editor would see, merging in the containers
 * and properties from compositions the way the backoffice does (by name).
 */
export function buildContentTypeLayout(
	type: USyncPreviewContentType,
	compositions: Map<string, USyncPreviewContentType>,
): USyncLayoutTab[] {
	const tabs = new Map<string, USyncLayoutTab>();

	const tabFor = (
		name: string,
		id: string | undefined,
		sortOrder: number,
		inherited: boolean,
	) => {
		let tab = tabs.get(name);
		if (!tab) {
			tab = { name, ids: [], inherited, sortOrder, properties: [], groups: [] };
			tabs.set(name, tab);
		}
		if (id) tab.ids.push(id);
		tab.inherited &&= inherited;
		return tab;
	};

	const groupFor = (
		tab: USyncLayoutTab,
		name: string,
		id: string,
		sortOrder: number,
		inherited: boolean,
	) => {
		let group = tab.groups.find((g) => g.name === name);
		if (!group) {
			group = { name, ids: [], inherited, sortOrder, properties: [] };
			tab.groups.push(group);
		}
		group.ids.push(id);
		group.inherited &&= inherited;
		return group;
	};

	for (const source of contributors(type, compositions)) {
		const inherited = source !== type;
		const containers = new Map(source.containers.map((c) => [c.id, c]));

		// where each container (and so each property) lands in the merged layout.
		const placed = new Map<string, USyncLayoutTab | USyncLayoutGroup>();

		for (const container of [...source.containers].sort(bySortOrder)) {
			if (container.type === 'Tab') {
				placed.set(
					container.id,
					tabFor(container.name ?? '', container.id, container.sortOrder, inherited),
				);
			}
		}

		for (const container of [...source.containers].sort(bySortOrder)) {
			if (container.type !== 'Group') continue;
			const parent = container.parent ? containers.get(container.parent.id) : undefined;
			const tab = parent
				? tabFor(parent.name ?? '', undefined, parent.sortOrder, inherited)
				: tabFor(USYNC_GENERIC_TAB, undefined, -1, inherited);
			placed.set(
				container.id,
				groupFor(tab, container.name ?? '', container.id, container.sortOrder, inherited),
			);
		}

		for (const property of [...source.properties].sort(bySortOrder)) {
			const target = property.container ? placed.get(property.container.id) : undefined;
			const entry = { property, inheritedFrom: inherited ? source.name : undefined };
			if (target) {
				target.properties.push(entry);
			} else {
				tabFor(USYNC_GENERIC_TAB, undefined, -1, inherited).properties.push(entry);
			}
		}
	}

	const result = [...tabs.values()].sort(bySortOrder);
	for (const tab of result) {
		tab.groups.sort(bySortOrder);
		tab.properties.sort((a, b) => a.property.sortOrder - b.property.sortOrder);
		for (const group of tab.groups) {
			group.properties.sort((a, b) => a.property.sortOrder - b.property.sortOrder);
		}
	}

	return result.filter((tab) => tab.properties.length > 0 || tab.groups.length > 0);
}

/** the type itself first, then everything it is composed of (depth first, no repeats). */
function contributors(
	type: USyncPreviewContentType,
	compositions: Map<string, USyncPreviewContentType>,
): USyncPreviewContentType[] {
	const seen = new Set<string>();
	const out: USyncPreviewContentType[] = [];

	const visit = (current: USyncPreviewContentType) => {
		if (seen.has(current.id)) return;
		seen.add(current.id);
		out.push(current);
		for (const id of compositionIds(current)) {
			const composition = compositions.get(id);
			if (composition) visit(composition);
		}
	};

	visit(type);
	return out;
}

function bySortOrder(a: { sortOrder: number }, b: { sortOrder: number }) {
	return a.sortOrder - b.sortOrder;
}
