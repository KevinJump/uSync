import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api'
import {
    ManifestMenuItem,
    umbExtensionsRegistry,
} from '@umbraco-cms/backoffice/extension-registry'
import {
    LitElement,
    customElement,
    html,
    property,
    state,
} from '@umbraco-cms/backoffice/external/lit'
import { UMB_SECTION_CONTEXT } from '@umbraco-cms/backoffice/section'

@customElement('usync-menu')
export class uSyncMenuElement extends UmbElementMixin(LitElement) {
    #pathName?: string

    @property({ type: Object, attribute: false })
    manifest!: ManifestMenuItem

    @state()
    hasChildren: boolean = false

    @state()
    itemPath?: string

    constructor() {
        super()

        umbExtensionsRegistry.byType('usync-menuItem').subscribe((_items) => {
            this.hasChildren = _items.length > 0
        })

        this.consumeContext(UMB_SECTION_CONTEXT, (sectionContext) => {
            this.observe(
                sectionContext?.pathname,
                (pathName) => {
                    this.#pathName = pathName
                    this.#constructHref()
                },
                'observePathname',
            )
        })
    }

    #constructHref() {
        if (!this.#pathName) return
        this.itemPath = `section/${this.#pathName}/workspace/${this.manifest.meta.entityType}`
    }

    render() {
        return html`<umb-menu-item-layout
            label=${this.manifest.meta.label ?? this.manifest.name}
            icon-name=${this.manifest.meta.icon ?? 'icon-bug'}
            .href=${this.itemPath}
            ?has-Children=${this.hasChildren}
            >${this.renderChildren()}
        </umb-menu-item-layout>`
    }

    renderChildren() {
        return html`<umb-extension-slot
            type="usync-menuItem"
            default-element="umb-menu-item-default"
        ></umb-extension-slot>`
    }
}

export default uSyncMenuElement
