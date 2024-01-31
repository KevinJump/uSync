var T = (s, i, t) => {
  if (!i.has(s))
    throw TypeError("Cannot " + t);
};
var e = (s, i, t) => (T(s, i, "read from private field"), t ? t.call(s) : i.get(s)), r = (s, i, t) => {
  if (i.has(s))
    throw TypeError("Cannot add the same private member more than once");
  i instanceof WeakSet ? i.add(s) : i.set(s, t);
}, n = (s, i, t, o) => (T(s, i, "write to private field"), o ? o.call(s, t) : i.set(s, t), t);
import { UmbBooleanState as w, UmbArrayState as O } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken as V } from "@umbraco-cms/backoffice/context-api";
import { u as y, O as v } from "./index-L8LKBc63.js";
import "@umbraco-cms/backoffice/resources";
import { UmbBaseController as g } from "@umbraco-cms/backoffice/class-api";
import { UMB_AUTH_CONTEXT as E } from "@umbraco-cms/backoffice/auth";
import "@umbraco-cms/backoffice/external/lit";
var m, u, p, f, l, h, c;
class U extends g {
  constructor(t) {
    super(t);
    r(this, m, void 0);
    r(this, u, void 0);
    r(this, p, void 0);
    r(this, f, void 0);
    r(this, l, void 0);
    r(this, h, void 0);
    r(this, c, void 0);
    n(this, u, new w(!1)), this.loaded = e(this, u).asObservable(), n(this, p, new O([], (o) => o.key)), this.actions = e(this, p).asObservable(), n(this, f, new O([], (o) => o.name)), this.currentAction = e(this, f).asObservable(), n(this, l, new w(!1)), this.working = e(this, l).asObservable(), n(this, h, new w(!1)), this.completed = e(this, h).asObservable(), n(this, c, new O([], (o) => o.name)), this.results = e(this, c).asObservable(), this.provideContext(k, this), n(this, m, new y(this)), this.consumeContext(E, (o) => {
      v.TOKEN = () => o.getLatestToken(), v.WITH_CREDENTIALS = !0, e(this, u).setValue(!0);
    });
  }
  async getActions() {
    const { data: t } = await e(this, m).getActions();
    t && e(this, p).setValue(t);
  }
  async performAction(t, o) {
    console.log("Perform Action:", t, o), e(this, l).setValue(!0), e(this, h).setValue(!1), e(this, c).setValue([]);
    var b = !1, d = "", A = 0;
    do {
      const { data: a } = await e(this, m).performAction(d, t, o, A);
      if (a) {
        A++, console.log(a);
        let C = a.status ?? [];
        e(this, f).setValue(C), d = a.requestId, b = a.complete, b && e(this, c).setValue((a == null ? void 0 : a.actions) ?? []);
      } else
        b = !0;
    } while (!b);
    e(this, h).setValue(!0), e(this, l).setValue(!1);
  }
}
m = new WeakMap(), u = new WeakMap(), p = new WeakMap(), f = new WeakMap(), l = new WeakMap(), h = new WeakMap(), c = new WeakMap();
const k = new V(U.name);
export {
  k as USYNC_CORE_CONTEXT_TOKEN,
  U as default,
  U as uSyncWorkspaceContext
};
//# sourceMappingURL=workspace.context-tRLO8UV-.js.map
