import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import {
	UmbConditionConfigBase,
	UmbConditionControllerArguments,
	UmbExtensionsManifestInitializer,
} from '@umbraco-cms/backoffice/extension-api';
import {
	UmbConditionBase,
	umbExtensionsRegistry,
} from '@umbraco-cms/backoffice/extension-registry';
import { UMB_SECTION_CONTEXT } from '@umbraco-cms/backoffice/section';
import { UMB_CURRENT_USER_CONTEXT } from '@umbraco-cms/backoffice/current-user';
import { USYNC_SECTION_ALIAS } from '../tree/constants';

export type SyncNewSectionConditionConfig = UmbConditionConfigBase & {};

export class SyncNewSectionCondition extends UmbConditionBase<SyncNewSectionConditionConfig> {
	#currentSection: string | undefined;
	#allSections: string[] | undefined;
	#userSections: string[] | undefined;

	constructor(
		host: UmbControllerHost,
		args: UmbConditionControllerArguments<SyncNewSectionConditionConfig>,
	) {
		super(host, args);

		new UmbExtensionsManifestInitializer(
			this,
			umbExtensionsRegistry,
			'section',
			() => true,
			async (sections) => {
				this.#allSections = sections.map((section) => section.alias);
				this.permitted = this.#checkPermission();
			},
			'uSyncAllSectionsManifestFilter',
		);

		this.consumeContext(UMB_CURRENT_USER_CONTEXT, (_context) => {
			if (!_context) return;

			this.observe(_context.allowedSections, (_sections) => {
				this.#userSections = _sections;
				this.permitted = this.#checkPermission();
			});
		});

		this.consumeContext(UMB_SECTION_CONTEXT, (_context) => {
			if (!_context) return;

			this.observe(_context.alias, (_alias) => {
				this.#currentSection = _alias;
				this.permitted = this.#checkPermission();
			});
		});
	}

	#checkPermission() {
		if (!this.#currentSection) return false;
		if (!this.#allSections) return false;

		// if the new section isn't defined, this will still show in settings.
		if (!this.#allSections.includes(USYNC_SECTION_ALIAS)) return true;

		if (this.#currentSection === USYNC_SECTION_ALIAS) return true;

		// the section exists, but this user hasn't been given it, so keep
		// showing uSync in settings (the api allows settings or usync access).
		if (this.#userSections && !this.#userSections.includes(USYNC_SECTION_ALIAS)) return true;

		return false;
	}
}
