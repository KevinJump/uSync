import { UmbElementMixin as c } from "@umbraco-cms/backoffice/element-api";
import { LitElement as m, html as u, customElement as p } from "@umbraco-cms/backoffice/external/lit";
var f = Object.defineProperty, v = Object.getOwnPropertyDescriptor, w = (r, t, s, n) => {
  for (var e = n > 1 ? void 0 : n ? v(t, s) : t, i = r.length - 1, l; i >= 0; i--)
    (l = r[i]) && (e = (n ? l(t, s, e) : l(e)) || e);
  return n && e && f(t, s, e), e;
};
let o = class extends c(m) {
  constructor() {
    super(), console.log("construct");
  }
  render() {
    return u`
            <h3>Settings view</h3>
        `;
  }
};
o = w([
  p("usync-settings-view")
], o);
const _ = o;
export {
  _ as default
};
//# sourceMappingURL=index-bYspDrjv.js.map
