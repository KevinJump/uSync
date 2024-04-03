import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { ManifestMenuItem, umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { LitElement, customElement, html, ifDefined, property, state } from "@umbraco-cms/backoffice/external/lit";
import { ManifestuSyncMenuItem } from "./types";

@customElement('usync-menu')
export class uSyncMenuElement extends UmbElementMixin(LitElement) {

    @property({type: Object, attribute: false})
    manifest!: ManifestMenuItem

    @state()
    hasChildren: boolean = false; 

    constructor() {
        super();
        
        umbExtensionsRegistry.byType('usync-menuItem').subscribe((_items) => {
            this.hasChildren = _items.length > 0;
        });
    }


    render() {
        return html`
            <umb-menu-item-layout
                label=${this.manifest.meta.label || this.manifest.name}
                icon-name=${this.manifest.meta.icon || 'icon-bug'}
                entity-type=${ifDefined(this.manifest.meta.entityType)}
                ?has-Children=${this.hasChildren}>${this.renderChildren()}</umb-menu-item-layout>`;
	}

    renderChildren() {
		return html` <umb-extension-slot
			type="usync-menuItem"
			.filter=${(items: ManifestuSyncMenuItem) => items.meta.menus.includes(this.manifest!.alias)}
    		default-element="umb-menu-item-default"></umb-extension-slot>`;
    }
}

export default uSyncMenuElement;