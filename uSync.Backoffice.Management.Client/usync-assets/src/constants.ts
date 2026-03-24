const _constants = {
	name: 'uSync',
	path: 'usync',
	icon: 'icon-infinity',
	menuName: 'Syncronisation',
	menuAlias: 'usync.menu',
	version: '17.x',

	conditions: {
		legacy: 'usync.legacy.condition',
	},

	workspace: {
		alias: 'usync.workspace',
		rootElement: 'usync-root',
		elementName: 'usync-workspace-root',
		contextAlias: 'usync.workspace.context',

		defaultView: {
			alias: 'usync.workspace.default',
		},

		settingView: {
			alias: 'usync.workspace.settings',
		},

		addOnView: {
			alias: 'usync.workspace.addons',
		},
		legacyView: {
			alias: 'usync.workspace.legacy',
		},
	},
};

export const uSyncConstants = _constants;

export const uSyncTimeFormatOptions: Intl.DateTimeFormatOptions = {
	year: 'numeric',
	month: 'short',
	day: 'numeric',
	hour: 'numeric',
	minute: 'numeric',
	second: 'numeric',
};
