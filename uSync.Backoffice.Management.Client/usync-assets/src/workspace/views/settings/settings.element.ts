import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	LitElement,
	css,
	customElement,
	html,
	state,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import {
	USYNC_CORE_CONTEXT_TOKEN,
	USyncHandlerSetSettings,
	USyncSettings,
} from '@jumoo/uSync';

export * from './components/usyncSettingItem.element.js';

@customElement('usync-settings-view')
export class USyncSettingsViewElement extends UmbElementMixin(LitElement) {
	@state()
	settings?: USyncSettings;

	@state()
	handlerSettings?: USyncHandlerSetSettings;

	constructor() {
		super();

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			if (!_instance) return;

			this.observe(_instance.settings, (_settings) => {
				if (!_settings) return;
				this.settings = _settings;
				_instance.getDefaultHandlerSetSettings(this.settings.defaultSet ?? 'Default');
			});

			this.observe(_instance.handlerSettings, (_handlerSettings) => {
				if (!_handlerSettings) return;
				this.handlerSettings = _handlerSettings;
			});

			_instance.getSettings();
		});
	}

	render() {
		return html`
			<umb-body-layout>
				<div class="usync-settings-layout">
					<div>
						<uui-box headline=${this.localize.termOrDefault('USyncSettings_settings', 'uSync Settings')}>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_processingMode', 'Processing Mode')}
								.description=${this.localize.termOrDefault('USyncSettings_processingModeDesc', 'How the uSync process runs, either in the background or interactively (Normal)')}
								.value=${this.settings?.processingMode}></usync-setting-item>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_importAtStartup', 'Import at startup')}
								.description=${this.localize.termOrDefault('USyncSettings_importAtStartupDesc', 'Run an import of files from the disk when Umbraco starts')}
								.value=${this.settings?.importAtStartup}></usync-setting-item>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_exportAtStartup', 'Export at startup')}
								.description=${this.localize.termOrDefault('USyncSettings_exportAtStartupDesc', 'Export the Umbraco settings when the site starts up')}
								.value=${this.settings?.exportAtStartup}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_exportOnSave', 'Export on save')}
								.description=${this.localize.termOrDefault('USyncSettings_exportOnSaveDesc', 'Generate uSync files when items are saved')}
								.value=${this.settings?.exportOnSave}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_uiEnabledGroups', 'UI Enabled groups')}
								.description=${this.localize.termOrDefault('USyncSettings_uiEnabledGroupsDesc', 'Handler groups that can be seen/used on the dashboard')}
								.value=${this.settings?.uiEnabledGroups}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_failOnMissingParent', 'Fail on missing parent')}
								.description=${this.localize.termOrDefault(
									'USyncSettings_failOnMissingParentDesc',
									'Fail on missing parent',
								)}
								.value=${this.settings?.failOnMissingParent}></usync-setting-item>
						</uui-box>

						<uui-box headline=${this.localize.termOrDefault('USyncSettings_filesAndFolders', 'File and folders')}>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_rootSite', 'Root Site')}
								.description=${this.localize.termOrDefault('USyncSettings_rootSiteDesc', 'Is this site a root for other sites.')}
								.value=${this.settings?.isRootSite}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_rootLocked', 'Root Locked')}
								.description=${this.localize.termOrDefault('USyncSettings_rootLockedDesc', 'Are changes for items that are from the root site locked?')}
								.value=${this.settings?.lockRoot}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_folders', 'Folders')}
								.description=${this.localize.termOrDefault('USyncSettings_foldersDesc', 'Folders uSync will look for files, (items are normally saved into last folder in the list)')}
								.value=${this.settings?.folders}></usync-setting-item>
						</uui-box>
					</div>

					<div>
						<uui-box headline=${this.localize.termOrDefault('USyncSettings_handlerDefaults', 'Handler defaults')}>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_handlerSet', 'Default handler set')}
								.description=${this.localize.termOrDefault('USyncSettings_handlerSetDesc', 'The default handler set to use for the site')}
								.value=${this.settings?.defaultSet}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_flatStructure', 'Flat structure')}
								.description=${this.localize.termOrDefault('USyncSettings_flatStructureDesc', 'All items of a type are stored in a flat folder structure')}
								.value=${this.handlerSettings?.handlerDefaults
									?.useFlatStructure}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_guidNames', 'Use guids for filenames')}
								.description=${this.localize.termOrDefault('USyncSettings_guidNamesDesc', 'Use the GUID of an item as the filename')}
								.value=${this.handlerSettings?.handlerDefaults
									?.guidNames}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_handlerGroups', 'Handler groups')}
								.description=${this.localize.termOrDefault('USyncSettings_handlerGroupsDesc', 'Groups to limit handler set to')}
								.value=${this.handlerSettings?.handlerDefaults
									?.group}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_failOnMissingParent', 'Fail on missing parent')}
								.description=${this.localize.termOrDefault(
									'USyncSettings_failOnMissingParentDesc',
									'Fail on missing parent',
								)}
								.value=${this.handlerSettings?.handlerDefaults
									?.failOnMissingParent}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_disabledHandlers', 'Disabled Handlers')}
								.description=${this.localize.termOrDefault('USyncSettings_disabledHandlersDesc', 'Handlers explicitly disabled for this handler set')}
								.value=${this.handlerSettings?.disabledHandlers}></usync-setting-item>
						</uui-box>
						<uui-box headline=${this.localize.termOrDefault('USyncSettings_bootSettings', 'First boot settings')}>
							<usync-setting-item
								.name=${this.localize.termOrDefault('USyncSettings_firstBoot', 'Import on First boot')}
								.description=${this.localize.termOrDefault('USyncSettings_firstBootDesc', 'Run the import process on first boot of the site')}
								.value=${this.settings?.importOnFirstBoot}></usync-setting-item>
							${when(
								this.settings?.importOnFirstBoot,
								() =>
									html` <usync-setting-item
										.name=${this.localize.termOrDefault('USyncSettings_firstBootGroup', 'First boot groups')}
										.description=${this.localize.termOrDefault('USyncSettings_firstBootGroupDesc', 'The groups to run on first boot')}
										.value=${this.settings?.firstBootGroup}></usync-setting-item>`,
							)}
						</uui-box>
					</div>
				</div>
				<div class="setting-link">
					<umb-localize key="USyncSettings_help"></umb-localize>
				</div>
			</umb-body-layout>
		`;
	}

	static styles = css`
		:host {
			display: block;
		}

		.usync-settings-layout {
			display: grid;
			grid-template-columns: 5fr 5fr;
			grid-template-rows: auto auto;
			gap: var(--uui-size-space-5);
			row-gap: var(--uui-size-space-5);
			grid-auto-flow: row;
			grid-template-areas: 'settings info', 'handler info';
		}

		.usync-settings-layout > div {
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-6);
		}

		.setting-link {
			text-align: center;
		}
	`;
}

export default USyncSettingsViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-settings-view': USyncSettingsViewElement;
	}
}
