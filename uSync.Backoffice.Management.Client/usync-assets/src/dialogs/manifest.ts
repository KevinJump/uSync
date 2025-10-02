const modal: UmbExtensionManifest = {
	type: 'modal',
	alias: 'usync.details.modal',
	name: 'usync details modal',
	js: () => import('./details-modal-element.js'),
};

const legacyModal: UmbExtensionManifest = {
	type: 'modal',
	alias: 'usync.legacy.modal',
	name: 'uSync legacy modal',
	js: () => import('./legacy-modal-element.js'),
};

const errorModal: UmbExtensionManifest = {
	type: 'modal',
	alias: 'usync.error.modal',
	name: 'uSync error modal',
	js: () => import('./error-modal-element.js'),
};

const singleImportModal: UmbExtensionManifest = {
	type: 'modal',
	alias: 'usync.import.single.modal',
	name: 'uSync single import modal',
	js: () => import('./single-import-modal.element.js'),
};

export const manifests = [modal, legacyModal, errorModal, singleImportModal];
