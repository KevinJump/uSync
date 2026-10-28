import { css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement, UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { SyncLegacyCheckResponse } from '@jumoo/uSync';

export interface uSyncLegacyModalValue {
	/** ids of the migrations to run as the legacy folder is copied. */
	migrations: Array<string>;
}

/**
 * confirms copying the legacy folder, and lets the user choose which migrations to run on the files.
 */
@customElement('usync-legacy-modal')
export class uSyncLegacyModalElement extends UmbModalBaseElement<
	SyncLegacyCheckResponse,
	uSyncLegacyModalValue
> {
	// migrations are all on until the user turns one off.
	@state()
	_skipped = new Set<string>();

	#onClose() {
		this.modalContext?.reject();
	}

	#onCopy() {
		const migrations = (this.data?.migrations ?? [])
			.map((migration) => migration.id)
			.filter((id) => !this._skipped.has(id));

		this.modalContext?.setValue({ migrations });
		this.modalContext?.submit();
	}

	#onToggleMigration(id: string, checked: boolean) {
		const skipped = new Set(this._skipped);
		if (checked) skipped.delete(id);
		else skipped.add(id);
		this._skipped = skipped;
	}

	render() {
		const version = this.data?.latestVersion;
		const migrations = this.data?.migrations ?? [];

		return html`
			<umb-body-layout
				.headline=${this.localize.termOrDefault('uSync_legacyCopyTitle', 'Overwrite files', version)}>
				<div class="content">
					<p>
						${this.localize.termOrDefault(
							'uSync_legacyCopyContent',
							'Are you sure you want to overwrite the contents of the folder with the legacy uSync folder files?',
							this.data?.latestFolder,
						)}
					</p>
					${this.renderMigrations(migrations)}
				</div>
				<div slot="actions">
					<uui-button
						id="cancel"
						label="cancel"
						@click=${this.#onClose}>${this.localize.term('general_cancel')}</uui-button>
					<uui-button id="copy" label="copy" look="primary" color="danger" @click=${this.#onCopy}
						>${migrations.length > 0 && migrations.some((m) => !this._skipped.has(m.id))
							? 'Migrate and copy'
							: 'Copy'}</uui-button
					>
				</div>
			</umb-body-layout>
		`;
	}

	renderMigrations(migrations: SyncLegacyCheckResponse['migrations']) {
		if (migrations.length == 0) {
			return nothing;
		}

		return html`
			<uui-box headline="Migrations">
				<p>
					These changes can be made to the files as they are copied, so the import that
					follows uses the current editors.
				</p>
				${migrations.map(
					(migration) => html`
						<div class="migration">
							<uui-checkbox
								label=${migration.name}
								?checked=${!this._skipped.has(migration.id)}
								@change=${(e: Event) =>
									this.#onToggleMigration(migration.id, (e.target as HTMLInputElement).checked)}
								>${migration.name} (${migration.count})</uui-checkbox
							>
							<div class="migration-description">${migration.description}</div>
							${migration.notice
								? html`<div class="migration-notice">${migration.notice}</div>`
								: nothing}
						</div>
					`,
				)}
			</uui-box>
		`;
	}

	static styles = css`
		.content {
			max-width: 520px;
		}

		.migration {
			margin-bottom: var(--uui-size-space-3);
		}

		.migration-description,
		.migration-notice {
			margin-left: var(--uui-size-space-6);
		}

		.migration-notice {
			color: var(--uui-color-text-alt);
			font-size: var(--uui-type-small-size);
		}
	`;
}

export default uSyncLegacyModalElement;

export const USYNC_LEGACY_MODAL = new UmbModalToken<SyncLegacyCheckResponse, uSyncLegacyModalValue>(
	'usync.legacy.modal',
	{
		modal: {
			type: 'dialog',
			size: 'small',
		},
	},
);
