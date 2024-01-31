import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api"
import { LitElement, customElement, html } from "@umbraco-cms/backoffice/external/lit";

@customElement('usync-settings-view')
export class uSyncSettingsViewElement extends UmbElementMixin(LitElement) {

    constructor() {
        super();
        console.log('construct');
    }

    render() {
        return html`
            <h3>Settings view</h3>
        `
    }
}

export default uSyncSettingsViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'usync-settings-view' : uSyncSettingsViewElement
    }
}