import {
	UUIIconRegistry,
	UUIIconRegistryElement,
} from '@umbraco-cms/backoffice/external/uui';
import { customElement } from '@umbraco-cms/backoffice/external/lit';

const icons = [
	{
		name: 'usync-logo',
		path: './icons/logo.js',
	},
];

export class uSyncIconRegistry extends UUIIconRegistry {
	protected acceptIcon(iconName: string): boolean {
		const iconManifest = icons.find((i) => i.name === iconName);
		if (!iconManifest) return false;

		const icon = this.provideIcon(iconName);
		const iconPath = iconManifest.path;

		import(/* @vite-ignore */ iconPath)
			.then((iconModule) => {
				icon.svg = iconModule.default;
			})
			.catch((err) => {
				console.log(`Failed to load icon ${iconName}`, err.message);
			});

		return true;
	}
}

@customElement('usync-icon-registry')
export class uSyncIconRegistryElement extends UUIIconRegistryElement {
	constructor() {
		super();
		this.registry = new uSyncIconRegistry();
	}
}

export default uSyncIconRegistry;
