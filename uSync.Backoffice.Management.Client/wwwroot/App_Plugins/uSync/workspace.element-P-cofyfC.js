import { UmbTextStyles as c } from "@umbraco-cms/backoffice/style";
import { UmbElementMixin as i } from "@umbraco-cms/backoffice/element-api";
import { LitElement as u, html as l, customElement as a } from "@umbraco-cms/backoffice/external/lit";
import "./default.element-epeWRxuL.js";
import "./action.context-03zE6EM4.js";
import "@umbraco-cms/backoffice/observable-api";
import "@umbraco-cms/backoffice/context-api";
import "./assets.js";
import "@umbraco-cms/backoffice/resources";
import "@umbraco-cms/backoffice/class-api";
var f = Object.defineProperty, y = Object.getOwnPropertyDescriptor, v = (n, r, s, t) => {
  for (var e = t > 1 ? void 0 : t ? y(r, s) : r, m = n.length - 1, p; m >= 0; m--)
    (p = n[m]) && (e = (t ? p(r, s, e) : p(e)) || e);
  return t && e && f(r, s, e), e;
};
let o = class extends i(u) {
  constructor() {
    super();
  }
  render() {
    return l`
            <umb-workspace-editor alias="usync.workspace" headline="uSync" .enforceNoFooter=${!0}>
                <usync-default-view></usync-default-view>
			</umb-workspace-editor>
        `;
  }
};
o.styles = [
  c
];
o = v([
  a("usync-workspace")
], o);
const j = o;
export {
  j as default,
  o as uSyncWorkspaceRootElement
};
//# sourceMappingURL=workspace.element-P-cofyfC.js.map
