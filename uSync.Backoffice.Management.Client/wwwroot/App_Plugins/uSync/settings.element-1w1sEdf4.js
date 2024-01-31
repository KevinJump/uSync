import { UmbElementMixin as i } from "@umbraco-cms/backoffice/element-api";
import { LitElement as m, html as u, customElement as p } from "@umbraco-cms/backoffice/external/lit";
var f = Object.defineProperty, v = Object.getOwnPropertyDescriptor, w = (c, t, r, n) => {
  for (var e = n > 1 ? void 0 : n ? v(t, r) : t, s = c.length - 1, o; s >= 0; s--)
    (o = c[s]) && (e = (n ? o(t, r, e) : o(e)) || e);
  return n && e && f(t, r, e), e;
};
let l = class extends i(m) {
  constructor() {
    super(), console.log("construct");
  }
  render() {
    return u`
            <h3>Settings view</h3>
        `;
  }
};
l = w([
  p("usync-settings-view")
], l);
const E = l;
export {
  E as default,
  l as uSyncSettingsViewElement
};
//# sourceMappingURL=settings.element-1w1sEdf4.js.map
