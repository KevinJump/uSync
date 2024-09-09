import {
	css,
	customElement,
	html,
	nothing,
	state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import {
	UMB_CONFIRM_MODAL,
	UMB_MODAL_MANAGER_CONTEXT,
} from '@umbraco-cms/backoffice/modal';
import {
	uSyncWorkspaceContext,
	USYNC_CORE_CONTEXT_TOKEN,
	SyncLegacyCheckResponse,
} from '@jumoo/uSync';

@customElement('usync-sync-legacy-files')
export class SyncLegacyFilesElement extends UmbLitElement {
	#actionContext?: uSyncWorkspaceContext;

	@state()
	_legacy?: SyncLegacyCheckResponse;

	constructor() {
		super();

		this.consumeContext(USYNC_CORE_CONTEXT_TOKEN, (_instance) => {
			this.#actionContext = _instance;

			this.observe(_instance.legacy, (_legacy) => {
				this._legacy = _legacy;
			});
		});
	}

	async #onCopy() {
		const modalContext = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);

		const confirmContext = modalContext?.open(this, UMB_CONFIRM_MODAL, {
			data: {
				headline: this.localize.term('uSync_legacyCopyTitle'),
				content: html`${this.localize.term('uSync_legacyCopyContent')}`,
				color: 'danger',
				confirmLabel: 'Copy',
			},
		});

		confirmContext
			?.onSubmit()
			.then(async () => {
				await this.#actionContext?.copyLegacy();
				window.location.reload();
			})
			.catch(() => {
				console.log('copy cancelled');
			});
	}

	async #onIgnore() {
		const modalContext = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
		const confirmContext = modalContext?.open(this, UMB_CONFIRM_MODAL, {
			data: {
				headline: this.localize.term('uSync_legacyIgnoreTitle'),
				content: html`${this.localize.term('uSync_legacyIgnoreContent')}`,
				color: 'danger',
				confirmLabel: 'Ignore',
			},
		});

		confirmContext
			?.onSubmit()
			.then(async () => {
				await this.#actionContext?.ignoreLegacy();
				window.location.reload();
			})
			.catch(() => {
				console.log('ignore cancelled');
			});
	}

	render() {
		return html`<umb-body-layout>
			${this.renderLegacyNote()} ${this.renderActions()}
		</umb-body-layout>`;
	}

	renderLegacyNote() {
		return html` <uui-box headline="Legacy uSync files detected">
			<p>
				uSync has found an old uSync folder at
				<strong>${this._legacy?.legacyFolder ?? ''}</strong>. It is likely that the
				content will need converting in some way.
			</p>
			${this.renderLegacyTypes(this._legacy?.legacyTypes)}
		</uui-box>`;
	}

	renderLegacyTypes(legacyTypes: Array<string> | undefined) {
		if (legacyTypes == undefined || legacyTypes.length == 0) {
			return nothing;
		}

		const legacyTypeHtml = legacyTypes.map((datatype) => {
			return html`<li>${datatype}</li>`;
		});

		return html`
			<div>
				<h3>Obsolete DataTypes</h3>
				<p>
					The following DataTypes - found in the legacy folder - are no longer supported
					in Umbraco 14 and will need to be converted.
				</p>
				<ul>
					${legacyTypeHtml}
				</ul>
				<p>
					You can convert these DataTypes using
					<a href="https://github.com/Jumoo/uSyncMigrations" target="_blank"
						>uSync.Migrations</a
					><br />
				</p>
			</div>
		`;
	}

	renderActions() {
		return html`
			<div class="legacy-actions">
				<uui-box>
					<div class="actions">
						<uui-button
							label="copy"
							color="positive"
							look="primary"
							@click=${this.#onCopy}
							>Overwrite v14 folder</uui-button
						>
						<p>Copy the contents of the legacy folder to the new uSync/v14 folder</p>
					</div>
				</uui-box>

				<uui-box>
					<div class="actions">
						<uui-button
							label="ignore"
							color="warning"
							look="primary"
							@click=${this.#onIgnore}
							>Ignore legacy folder</uui-button
						>
						<p>Ignore the legacy folder and continue</p>
					</div>
				</uui-box>
			</div>
		`;
	}

	static styles = css`
		uui-box {
			margin-bottom: var(--uui-size-3);
		}

		.legacy-actions {
			display: flex;
			gap: var(--uui-size-space-4);
			justify-content: space-between;
		}

		.legacy-actions uui-box {
			flex-basis: 50%;
		}

		.legacy-actions .actions {
			display: flex;
			flex-direction: column;
			align-items: center;
		}

		uui-button {
			font-size: var(--uui-type-h5-size);
		}
	`;
}

export default SyncLegacyFilesElement;
