import { UmbLitElement as h } from "@umbraco-cms/backoffice/lit-element";
import { html as p, css as v, property as u, customElement as y } from "@umbraco-cms/backoffice/external/lit";
import { T as _ } from "./consts-BD6tjpOQ.js";
var f = Object.defineProperty, b = Object.getOwnPropertyDescriptor, c = (t) => {
  throw TypeError(t);
}, d = (t, e, a, s) => {
  for (var r = s > 1 ? void 0 : s ? b(e, a) : e, o = t.length - 1, n; o >= 0; o--)
    (n = t[o]) && (r = (s ? n(e, a, r) : n(r)) || r);
  return s && r && f(e, a, r), r;
}, $ = (t, e, a) => e.has(t) || c("Cannot " + a), C = (t, e, a) => e.has(t) ? c("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, a), E = (t, e, a) => ($(t, e, "access private method"), a), l, m;
let i = class extends h {
  constructor() {
    super(...arguments), C(this, l);
  }
  render() {
    var t, e, a, s;
    return p`<umb-body-layout
      ><div slot="header">
        <h3>
          ${((t = this.data) == null ? void 0 : t.item.method) ?? "History"} @
          ${this.localize.date(((e = this.data) == null ? void 0 : e.item.date) ?? "", _)}
        </h3>
        ${(a = this.data) == null ? void 0 : a.item.username}
      </div>
      <div class="layout">
        <usync-results
          .hideActions=${!0}
          .hideResultBar=${!0}
          .results=${((s = this.data) == null ? void 0 : s.item.actions) ?? []}
        ></usync-results>
      </div>
      <div slot="actions">
        <uui-button
          id="cancel"
          .label=${this.localize.term("general_close")}
          @click="${E(this, l, m)}"
        ></uui-button></div
    ></umb-body-layout>`;
  }
};
l = /* @__PURE__ */ new WeakSet();
m = function() {
  var t;
  (t = this.modalContext) == null || t.submit();
};
i.styles = v`
    h3 {
      margin: 0px;
    }
  `;
d([
  u({ attribute: !1 })
], i.prototype, "modalContext", 2);
d([
  u({ attribute: !1 })
], i.prototype, "data", 2);
i = d([
  y("history-sidebar")
], i);
const w = i;
export {
  i as HistorySidebarElement,
  w as default
};
//# sourceMappingURL=history-modal.element-CoNqy51l.js.map
