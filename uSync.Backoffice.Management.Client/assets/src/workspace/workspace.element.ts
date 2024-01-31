import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { LitElement, customElement, html } from "@umbraco-cms/backoffice/external/lit";

import './views/default/default.element';

@customElement('usync-workspace')
export class uSyncWorkspaceRootElement extends UmbElementMixin(LitElement)
{
    constructor() {
        super();
    }

    render() {
        return html`
            <umb-workspace-editor alias="usync.workspace" headline="uSync" .enforceNoFooter=${true}>
                <div slot="header">v14.0.0-early</div>
                <usync-default-view></usync-default-view>
			</umb-workspace-editor>
        `;
    }   

    static styles = [
		UmbTextStyles		
	];
}

export default uSyncWorkspaceRootElement;

declare global {
    interface HTMLElementTagNameMap {
        'usync-workspace' : uSyncWorkspaceRootElement;
    }
}