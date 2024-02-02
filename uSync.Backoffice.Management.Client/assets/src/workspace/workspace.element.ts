import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { LitElement, customElement, html } from "@umbraco-cms/backoffice/external/lit";

import './views/default/default.element';
import uSyncWorkspaceContext from './workspace.context';

@customElement('usync-workspace-root')
export class uSyncWorkspaceRootElement extends UmbElementMixin(LitElement) {
    #workspaceContext = new uSyncWorkspaceContext(this);

    render() {
        return html`
            <umb-workspace-editor alias="usync.workspace" headline="uSync" .enforceNoFooter=${true}>
                <div slot="header">v14.0.0-early</div>
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
        'usync-workspace-root' : uSyncWorkspaceRootElement;
    }
}