const _constants = {
	name: 'uSync',
	path: 'usync',
	icon: 'icon-infinity',
	menuName: 'Syncronisation',
	menuAlias: 'usync.menu',
	version: '14.0.0-rc2',

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
	},
};

export const uSyncConstants = _constants;
