import type { ManifestuSyncItemPreview } from './types.js';

const dataTypePreview: ManifestuSyncItemPreview = {
	type: 'usync-item-preview',
	alias: 'usync.item-preview.data-type',
	name: 'uSync data type preview',
	element: () => import('../components/usync-data-type-preview.js'),
	forEntityTypes: ['data-type'],
};

const contentTypePreview: ManifestuSyncItemPreview = {
	type: 'usync-item-preview',
	alias: 'usync.item-preview.content-type',
	name: 'uSync content type preview',
	element: () => import('../components/usync-content-type-preview.js'),
	forEntityTypes: ['document-type', 'media-type', 'member-type'],
};

const contentPreview: ManifestuSyncItemPreview = {
	type: 'usync-item-preview',
	alias: 'usync.item-preview.content',
	name: 'uSync content preview',
	element: () => import('../components/usync-content-preview.js'),
	forEntityTypes: ['document', 'media'],
};

export const manifests = [dataTypePreview, contentTypePreview, contentPreview];
