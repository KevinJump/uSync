import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { LitElement, customElement, html } from "@umbraco-cms/backoffice/external/lit";

import { USYNC_ACTION_CONTEXT_TOKEN, uSyncWorkspaceActionContext } from './context/action.context';

import './views/default/default.element';
import { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';

@customElement('usync-workspace')
export class uSyncWorkspaceRootElement extends UmbElementMixin(LitElement)
{
    constructor() {
        super();
    }

    render() {
        return html`
            <umb-workspace-editor alias="usync.workspace" headline="uSync" .enforceNoFooter=${true}>
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