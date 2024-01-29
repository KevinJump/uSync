var m = (o, e, t) => {
  if (!e.has(o))
    throw TypeError("Cannot " + t);
};
var r = (o, e, t) => (m(o, e, "read from private field"), t ? t.call(o) : e.get(o)), a = (o, e, t) => {
  if (e.has(o))
    throw TypeError("Cannot add the same private member more than once");
  e instanceof WeakSet ? e.add(o) : e.set(o, t);
}, c = (o, e, t, s) => (m(o, e, "write to private field"), s ? s.call(o, t) : e.set(o, t), t);
import { UmbArrayState as p } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken as h } from "@umbraco-cms/backoffice/context-api";
import { uSyncActionRepository as l } from "./assets.js";
import "@umbraco-cms/backoffice/resources";
import { UmbBaseController as f } from "@umbraco-cms/backoffice/class-api";
var i, n;
class y extends f {
  constructor(t) {
    super(t);
    a(this, i, void 0);
    a(this, n, void 0);
    c(this, n, new p([], (s) => s.key)), this.actions = r(this, n).asObservable(), this.provideContext(A, this), c(this, i, new l(this));
  }
  async getActions() {
    const { data: t } = await r(this, i).getActions();
    t && r(this, n).setValue(t);
  }
  async getTime() {
    const { data: t } = await r(this, i).getTime();
    t && console.log(t);
  }
  performAction(t, s) {
    console.log("Perform Action:", t, s);
  }
}
i = new WeakMap(), n = new WeakMap();
const A = new h(y.name);
export {
  A as USYNC_ACTION_CONTEXT_TOKEN,
  y as default,
  y as uSyncWorkspaceActionContext
};
//# sourceMappingURL=action.context-03zE6EM4.js.map
