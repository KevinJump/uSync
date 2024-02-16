import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api"
import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { USYNC_CORE_CONTEXT_TOKEN } from "../../workspace.context.ts";
import { uSyncHandlerSetSettings, uSyncSettings } from "../../../api/index.ts";

export * from './components/usyncSettingItem.element.ts';

@customElement('usync-settings-view')
export class uSyncSettingsViewElement extends UmbElementMixin(LitElement) {

    @state()
    settings?: uSyncSettings

    @state()
    handlerSettings? : uSyncHandlerSetSettings

    constructor() {
        super();

        this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {

            this.observe(_instance.settings, (_settings) => {
                this.settings = _settings;
            });

            this.observe(_instance.handlerSettings, (_handlerSettings) => {
                this.handlerSettings = _handlerSettings;
            })

            this.observe(_instance.loaded, (_loaded) => {

                _instance.getSettings();
                _instance.getDefaultHandlerSetSettings();

            });
        });

    }

    render() {
        return html`
            <div class="usync-settings-layout">
                <div>
                    <uui-box headline=${this.localize.term('uSyncSettings_settings')}>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_importAtStartup')}
                            .description=${this.localize.term('uSyncSettings_importAtStartupDesc')}
                            .value=${this.settings?.importAtStartup}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_exportAtStartup')}
                            .description=${this.localize.term('uSyncSettings_exportAtStartupDesc')}
                            .value=${this.settings?.exportAtStartup}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_exportOnSaveup')}
                            .description=${this.localize.term('uSyncSettings_exportOnSaveDesc')}
                            .value=${this.settings?.exportOnSave}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_uiEnabledGroups')}
                            .description=${this.localize.term('uSyncSettings_uiEnabledGroupsDesc')}
                            .value=${this.settings?.uiEnabledGroups}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_failOnMissingParent')}
                            .description=${this.localize.term('uSyncSettings_failOnMissingParentDesc')}
                            .value=${this.settings?.failOnMissingParent}></usync-setting-item>
                    </uui-box>

                    <uui-box headline=${this.localize.term('uSyncSettings_filesAndFolders')}>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_rootSite')}
                            .description=${this.localize.term('uSyncSettings_rootSiteDesc')}
                            .value=${this.settings?.isRootSite}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_rootLocked')}
                            .description=${this.localize.term('uSyncSettings_rootLockedDesc')}
                            .value=${this.settings?.lockRoot}></usync-setting-item>

                        <usync-setting-item
                            .name=${this.localize.term('uSyncSettings_folders')}
                            .description=${this.localize.term('uSyncSettings_foldersDesc')}
                            .value=${this.settings?.folders}></usync-setting-item>

                    </uui-box>
                </div>

                <div>
                    <uui-box headline=${this.localize.term('uSyncSettings_handlerDefaults')}>

                    <usync-setting-item
                        .name=${this.localize.term('uSyncSettings_flatStructure')}
                        .description=${this.localize.term('uSyncSettings_flatStructureDesc')}
                        .value=${this.handlerSettings?.handlerDefaults?.useFlatStructure}></usync-setting-item>

                    <usync-setting-item
                        .name=${this.localize.term('uSyncSettings_guidNames')}
                        .description=${this.localize.term('uSyncSettings_guidNamesDesc')}
                        .value=${this.handlerSettings?.handlerDefaults?.guidNames}></usync-setting-item>

                    <usync-setting-item
                        .name=${this.localize.term('uSyncSettings_handlerGroups')}
                        .description=${this.localize.term('uSyncSettings_handlerGroupsDesc')}
                        .value=${this.handlerSettings?.handlerDefaults?.group}></usync-setting-item>

                    <usync-setting-item
                        .name=${this.localize.term('uSyncSettings_failOnMissingParent')}
                        .description=${this.localize.term('uSyncSettings_failOnMissingParentDesc')}
                        .value=${this.handlerSettings?.handlerDefaults?.failOnMissingParent}></usync-setting-item>

                    <usync-setting-item
                        .name=${this.localize.term('uSyncSettings_disabledHandlers')}
                        .description=${this.localize.term('uSyncSettings_disabledHandlersDesc')}
                        .value=${this.handlerSettings?.disabledHandlers}></usync-setting-item>
                    </uui-box>


                </div>
            </div>
            <div class="setting-link">
                    <umb-localize key="uSyncSettings_help"></umb-localize>
            </div>
        `
    }

    static styles = css`
        :host {
            display: block;
            margin: var(--uui-size-layout-1);
        }

        .usync-settings-layout {
            display: grid;
            grid-template-columns: 5fr 5fr;
            grid-template-rows: auto auto;
            gap: 20px 20px;
            grid-auto-flow: row;
            grid-template-areas:
                'settings info',
                'handler info';
        }

        .setting-link {
            text-align: center;
        }

        uui-box {
            margin-bottom: var(--uui-size-layout-1);
        }
    `
}

export default uSyncSettingsViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'usync-settings-view': uSyncSettingsViewElement
    }
}