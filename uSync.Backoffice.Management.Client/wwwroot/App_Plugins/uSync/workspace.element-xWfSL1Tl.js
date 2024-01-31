import { UmbTextStyles as n } from "@umbraco-cms/backoffice/style";
import { UmbElementMixin as c } from "@umbraco-cms/backoffice/element-api";
import { LitElement as l, html as u, customElement as a } from "@umbraco-cms/backoffice/external/lit";
import "./default.element-M-mc6NXl.js";
import "./workspace.context-tRLO8UV-.js";
import "@umbraco-cms/backoffice/observable-api";
import "@umbraco-cms/backoffice/context-api";
import "./index-L8LKBc63.js";
import "@umbraco-cms/backoffice/resources";
import "@umbraco-cms/backoffice/class-api";
import "@umbraco-cms/backoffice/auth";
var f = Object.defineProperty, v = Object.getOwnPropertyDescriptor, y = (i, r, s, t) => {
  for (var e = t > 1 ? void 0 : t ? v(r, s) : r, m = i.length - 1, p; m >= 0; m--)
    (p = i[m]) && (e = (t ? p(r, s, e) : p(e)) || e);
  return t && e && f(r, s, e), e;
};
let o = class extends c(l) {
  constructor() {
    super();
  }
  render() {
    return u`
            <umb-workspace-editor alias="usync.workspace" headline="uSync" .enforceNoFooter=${!0}>
                <div slot="header">v14.0.0-early</div>
                <usync-default-view></usync-default-view>
			</umb-workspace-editor>
        `;
  }
};
o.styles = [
  n
];
o = y([
  a("usync-workspace")
], o);
const D = o;
export {
  D as default,
  o as uSyncWorkspaceRootElement
};
//# sourceMappingURL=workspace.element-xWfSL1Tl.js.map
