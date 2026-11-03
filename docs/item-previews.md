# Item previews

The details dialog shows the current and incoming versions of an item side by side, using the real
property editors. This page explains how a package adds previews for its own items.

A preview takes four pieces. Each one is found by type, so nothing in uSync needs to know about
your items.

## 1. Handler: build the preview

Override `GetPreviewAsync` on your handler and call `BuildPreviewAsync`:

```csharp
public override Task<SyncItemPreview?> GetPreviewAsync(XElement node, HandlerSettings settings, SyncPreviewOptions? options = null)
    => BuildPreviewAsync(node, settings, SerializerFlags.None, options);
```

This runs your serializer as a dry run, inside a scope that is never completed. Handlers that don't
override it return `null`, and the dialog shows only the Diff tab.

## 2. Serializer: honour `DoNotSave`

The dry run passes `SerializerFlags.DoNotSave`. The serializer must build the item in memory and
return it in the attempt without saving it. Read the flag with `options.Flags.HasFlag(SerializerFlags.DoNotSave)`.

If the item uses things that aren't in the site yet, call `options.PendingItemResolver(type, key)`
to build them from the uSync files. They end up in `SyncItemPreview.PendingItems`. For example,
a new data type used by a new property.

## 3. Mapper: shape it for the backoffice

Add an `ISyncPreviewMapper`. The type loader finds it, so there's nothing to register. The base
class matches on the item type and gives you the typed items:

```csharp
public class FormPreviewMapper : SyncPreviewMapperBase<Form>
{
    public override string EntityType => "forms-form";

    protected override Task<object?> MapAsync(SyncItemPreview preview, Form? current, Form? incoming)
        => Task.FromResult<object?>(new { current, incoming });
}
```

- `EntityType` is how the backoffice picks the element.
- The object you return is sent to the backoffice as `preview` on the response.
- Mappers run inside the preview scope, so they can read the database and see what the dry run built.

uSync picks the first mapper that can map a preview. To replace one of the uSync mappers, give
yours a lower `[Weight]` (the uSync ones use the default).

`uSync.Backoffice.Management.Api` has `ISyncPreviewMappingHelper` for items with properties. It
maps compositions, the data types the properties use (including new ones), and the content info
(parent, sort order, published state).

## 4. Element: show it

Register a `usync-item-preview` element for the entity type. It gets the whole response as `preview`:

```ts
import type { ManifestuSyncItemPreview } from '@jumoo/uSync';

export const manifests: Array<ManifestuSyncItemPreview> = [
	{
		type: 'usync-item-preview',
		alias: 'my.package.form-preview',
		name: 'Form preview',
		element: () => import('./form-preview.element.js'),
		forEntityTypes: ['forms-form'],
	},
];
```

```ts
@customElement('my-form-preview')
export class MyFormPreviewElement extends UmbLitElement {
	@property({ type: Object })
	preview?: SyncItemPreviewResponse;

	render() {
		const model = getPreviewModel<MyFormPreview>(this.preview);
		// ...
	}
}
```

The Changes tab only shows when an element is registered for the entity type. When more than one
matches, the one with the highest manifest `weight` wins.

The uSync previews are registered the same way (`src/preview/manifest.ts`). Their models are typed
in `@jumoo/uSync`: `SyncDataTypePreview`, `SyncContentTypePreview` and `SyncContentPreview`.
