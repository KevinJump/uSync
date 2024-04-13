import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	LitElement,
	css,
	customElement,
	html,
} from '@umbraco-cms/backoffice/external/lit';

import './views/default/default.element';
import uSyncWorkspaceContext from './workspace.context';
import { uSyncConstants } from '../constants';

@customElement('usync-workspace-root')
export class uSyncWorkspaceRootElement extends UmbElementMixin(LitElement) {
	#workspaceContext: uSyncWorkspaceContext;

	constructor() {
		super();

		this.#workspaceContext = new uSyncWorkspaceContext(this);

		this.observe(this.#workspaceContext.completed, (_completed) => {
			// console.log('completed', _completed);
		});
	}

	render() {
		return html`
			<umb-workspace-editor .enforceNoFooter=${true}>
				<div slot="header" class="header">
					<div>
						<strong><umb-localize key="uSync_name"></umb-localize></strong><br /><em
							>(${uSyncConstants.version})</em
						>
					</div>
				</div>
			</umb-workspace-editor>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			umb-workspace-editor > div.header {
				display: flex;
				align-items: center;
				align-content: center;
			}

			.header > div {
				padding-right: 20px;
			}

			uui-icon {
				font-size: 16pt;
			}
		`,
	];
}

export default uSyncWorkspaceRootElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-workspace-root': uSyncWorkspaceRootElement;
	}
}
