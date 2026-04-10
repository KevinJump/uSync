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

export * from './components/usyncSettingItem.element';

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
						<uui-box headline=${this.localize.term('USyncSettings_settings')}>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_processingMode')}
								.description=${this.localize.term('USyncSettings_processingModeDesc')}
								.value=${this.settings?.processingMode}></usync-setting-item>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_importAtStartup')}
								.description=${this.localize.term('USyncSettings_importAtStartupDesc')}
								.value=${this.settings?.importAtStartup}></usync-setting-item>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_exportAtStartup')}
								.description=${this.localize.term('USyncSettings_exportAtStartupDesc')}
								.value=${this.settings?.exportAtStartup}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_exportOnSave')}
								.description=${this.localize.term('USyncSettings_exportOnSaveDesc')}
								.value=${this.settings?.exportOnSave}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_uiEnabledGroups')}
								.description=${this.localize.term('USyncSettings_uiEnabledGroupsDesc')}
								.value=${this.settings?.uiEnabledGroups}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_failOnMissingParent')}
								.description=${this.localize.term(
									'USyncSettings_failOnMissingParentDesc',
								)}
								.value=${this.settings?.failOnMissingParent}></usync-setting-item>
						</uui-box>

						<uui-box headline=${this.localize.term('USyncSettings_filesAndFolders')}>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_rootSite')}
								.description=${this.localize.term('USyncSettings_rootSiteDesc')}
								.value=${this.settings?.isRootSite}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_rootLocked')}
								.description=${this.localize.term('USyncSettings_rootLockedDesc')}
								.value=${this.settings?.lockRoot}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_folders')}
								.description=${this.localize.term('USyncSettings_foldersDesc')}
								.value=${this.settings?.folders}></usync-setting-item>
						</uui-box>
					</div>

					<div>
						<uui-box headline=${this.localize.term('USyncSettings_handlerDefaults')}>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_handlerSet')}
								.description=${this.localize.term('USyncSettings_handlerSetDesc')}
								.value=${this.settings?.defaultSet}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_flatStructure')}
								.description=${this.localize.term('USyncSettings_flatStructureDesc')}
								.value=${this.handlerSettings?.handlerDefaults
									?.useFlatStructure}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_guidNames')}
								.description=${this.localize.term('USyncSettings_guidNamesDesc')}
								.value=${this.handlerSettings?.handlerDefaults
									?.guidNames}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_handlerGroups')}
								.description=${this.localize.term('USyncSettings_handlerGroupsDesc')}
								.value=${this.handlerSettings?.handlerDefaults
									?.group}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_failOnMissingParent')}
								.description=${this.localize.term(
									'USyncSettings_failOnMissingParentDesc',
								)}
								.value=${this.handlerSettings?.handlerDefaults
									?.failOnMissingParent}></usync-setting-item>

							<usync-setting-item
								.name=${this.localize.term('USyncSettings_disabledHandlers')}
								.description=${this.localize.term('USyncSettings_disabledHandlersDesc')}
								.value=${this.handlerSettings?.disabledHandlers}></usync-setting-item>
						</uui-box>
						<uui-box headline=${this.localize.term('USyncSettings_bootSettings')}>
							<usync-setting-item
								.name=${this.localize.term('USyncSettings_firstBoot')}
								.description=${this.localize.term('USyncSettings_firstBootDesc')}
								.value=${this.settings?.importOnFirstBoot}></usync-setting-item>
							${when(
								this.settings?.importOnFirstBoot,
								() =>
									html` <usync-setting-item
										.name=${this.localize.term('USyncSettings_firstBootGroup')}
										.description=${this.localize.term('USyncSettings_firstBootGroupDesc')}
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
