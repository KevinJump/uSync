import { UmbElementMixin as x } from "@umbraco-cms/backoffice/element-api";
import { css as f, property as p, customElement as h, LitElement as y, html as c } from "@umbraco-cms/backoffice/external/lit";
import { USYNC_ACTION_CONTEXT_TOKEN as g } from "./action.context-03zE6EM4.js";
import "@umbraco-cms/backoffice/observable-api";
import "@umbraco-cms/backoffice/context-api";
import "./assets.js";
import "@umbraco-cms/backoffice/resources";
import "@umbraco-cms/backoffice/class-api";
var _ = Object.defineProperty, b = Object.getOwnPropertyDescriptor, d = (o, t, e, i) => {
  for (var r = i > 1 ? void 0 : i ? b(t, e) : t, s = o.length - 1, n; s >= 0; s--)
    (n = o[s]) && (r = (i ? n(t, e, r) : n(r)) || r);
  return i && r && _(t, e, r), r;
};
let u = class extends y {
  constructor() {
    super(...arguments), this.myName = "";
  }
  _handleClick(o, t) {
    this.dispatchEvent(new CustomEvent("perform-action", {
      detail: {
        group: o,
        key: t.key
      }
    }));
  }
  render() {
    var e, i, r, s;
    const o = (e = this.group) == null ? void 0 : e.key, t = (i = this.group) == null ? void 0 : i.buttons.map((n) => c`
                <uui-button label=${n.key} 
                    color=${n.color}
                    look=${n.look}
                    style="font-size: 20px"
                    @click=${() => this._handleClick(o, n)}
                    ></uui-button>
            `);
    return c`
                <uui-box class='action-box'>

                    <div class="box-content">

                        <h2 class="box-heading">${(r = this.group) == null ? void 0 : r.groupName}</h2>

                        <uui-icon name=${(s = this.group) == null ? void 0 : s.icon}></uui-icon>
                    
                        <div class="box-buttons">
                            ${t}
                        </div>
                        
                    </div>
                </uui-box>
        `;
  }
};
u.styles = f`

        .box-content {
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .box-heading {
            font-size: 20pt;
        }

        uui-icon {
            margin: 20px;
            font-size: 40pt;
        }

        uui-button {
            margin: 0 5px;
        }

        .box-buttons {
            margin-top: 10px;
        }
        `;
d([
  p({ type: String })
], u.prototype, "myName", 2);
d([
  p({ type: Object })
], u.prototype, "group", 2);
u = d([
  h("usync-action-box")
], u);
var $ = Object.defineProperty, C = Object.getOwnPropertyDescriptor, m = (o, t, e, i) => {
  for (var r = i > 1 ? void 0 : i ? C(t, e) : t, s = o.length - 1, n; s >= 0; s--)
    (n = o[s]) && (r = (i ? n(t, e, r) : n(r)) || r);
  return i && r && $(t, e, r), r;
}, v = (o, t, e) => {
  if (!t.has(o))
    throw TypeError("Cannot " + e);
}, w = (o, t, e) => (v(o, t, "read from private field"), e ? e.call(o) : t.get(o)), O = (o, t, e) => {
  if (t.has(o))
    throw TypeError("Cannot add the same private member more than once");
  t instanceof WeakSet ? t.add(o) : t.set(o, e);
}, E = (o, t, e, i) => (v(o, t, "write to private field"), i ? i.call(o, e) : t.set(o, e), e), l;
let a = class extends x(y) {
  constructor() {
    super(), O(this, l, void 0), this.loaded = !1, this.consumeContext(g, (o) => {
      console.log("consume context"), E(this, l, o), this.observe(o.actions, (t) => {
        console.log("actions", t), this.actions = t, this.loaded = this.actions !== null;
      }), o.getActions();
    });
  }
  /**
   * 
   * @param event 
   */
  performAction(o) {
    var t;
    (t = w(this, l)) == null || t.performAction(o.detail.group, o.detail.key);
  }
  render() {
    var t, e;
    if (this.loaded == !1)
      return c`<uui-loader></uui-loader>`;
    console.log((t = this.actions) == null ? void 0 : t.length);
    var o = (e = this.actions) == null ? void 0 : e.map((i) => c`
                <usync-action-box myName="fred"
                    .group="${i}"
                    @perform-action=${this.performAction}>
                </usync-action-box>
            `);
    return c`
                <div class="action-buttons-box">
                    ${o}
                </div>
            `;
  }
};
l = /* @__PURE__ */ new WeakMap();
a.styles = [
  f`
            usync-action-box {
               margin: var(--uui-size-layout-1);
            }

            .action-buttons-box {
               margin: var(--uui-size-layout-1);
               display: grid;
               grid-template-columns: 1fr 1fr 1fr;
            }        
        `
];
m([
  p({ type: Array })
], a.prototype, "actions", 2);
m([
  p({ type: Boolean })
], a.prototype, "loaded", 2);
a = m([
  h("usync-default-view")
], a);
const B = a;
export {
  B as default,
  a as uSyncDefaultViewElement
};
//# sourceMappingURL=default.element-epeWRxuL.js.map
