import type { ManifestElement } from '@umbraco-cms/backoffice/extension-api';
import type {
	DataTypeResponseModel,
	DocumentResponseModel,
	DocumentTypeResponseModel,
	MediaResponseModel,
	MediaTypeResponseModel,
	MemberTypeResponseModel,
} from '@umbraco-cms/backoffice/external/backend-api';
import type { SyncItemPreviewResponse } from '../api';

/**
 * An element that shows the preview of an import for one or more entity types.
 */
export interface uSyncItemPreviewElement extends HTMLElement {
	preview?: SyncItemPreviewResponse;
}

/**
 * Shows the preview for items of the given entity types in the details dialog.
 *
 * The entity types match the `EntityType` of the server side preview mapper,
 * and `preview.preview` is the model that mapper built.
 */
export interface ManifestuSyncItemPreview extends ManifestElement<uSyncItemPreviewElement> {
	type: 'usync-item-preview';
	forEntityTypes: Array<string>;
}

declare global {
	interface UmbExtensionManifestMap {
		ManifestuSyncItemPreview: ManifestuSyncItemPreview;
	}
}

/** the model a preview mapper built, as the type the element expects. */
export const getPreviewModel = <T>(preview?: SyncItemPreviewResponse) =>
	preview?.preview as T | undefined;

export type uSyncPreviewContentTypeModel =
	| DocumentTypeResponseModel
	| MediaTypeResponseModel
	| MemberTypeResponseModel;

/** preview model for a data type (entity type data-type) */
export interface SyncDataTypePreview {
	current?: DataTypeResponseModel | null;
	incoming?: DataTypeResponseModel | null;
}

/** what the preview needs to lay out the properties of an item */
export interface SyncPropertiesPreview<TTypeModel> {
	/** the content types either side is composed of (as they are in the site) */
	compositions: Array<TTypeModel>;
	/** the data types used by the properties on either side */
	dataTypes: Array<DataTypeResponseModel>;
	/** keys of the data types that aren't in the site yet */
	newDataTypes: Array<string>;
	/** keys of the compositions that aren't in the site yet */
	newCompositions: Array<string>;
}

/** preview model for a document, media or member type */
export interface SyncContentTypePreview<
	TTypeModel extends uSyncPreviewContentTypeModel = uSyncPreviewContentTypeModel,
> extends SyncPropertiesPreview<TTypeModel> {
	current?: TTypeModel | null;
	incoming?: TTypeModel | null;
}

export interface SyncContentPreviewCulture {
	culture: string;
	published: boolean;
}

/** the settings of a content or media item that aren't property values */
export interface SyncContentPreviewInfo {
	/** name of the parent item (null when the item is at the root) */
	parent?: string | null;
	sortOrder: number;
	trashed: boolean;
	/** alias of the template (content only) */
	template?: string | null;
	/** published state for each culture (an empty culture for invariant content) */
	published: Array<SyncContentPreviewCulture>;
}

/** preview model for a document or media item */
export interface SyncContentPreview<
	TModel extends DocumentResponseModel | MediaResponseModel = DocumentResponseModel | MediaResponseModel,
	TTypeModel extends DocumentTypeResponseModel | MediaTypeResponseModel =
		| DocumentTypeResponseModel
		| MediaTypeResponseModel,
> extends SyncPropertiesPreview<TTypeModel> {
	current?: TModel | null;
	incoming?: TModel | null;
	currentInfo?: SyncContentPreviewInfo | null;
	incomingInfo?: SyncContentPreviewInfo | null;
	/** the document (or media) type of the item, as it is in the site */
	contentType?: TTypeModel | null;
}
