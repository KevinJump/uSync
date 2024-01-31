var T = (s, o, t) => {
  if (!o.has(s))
    throw TypeError("Cannot " + t);
};
var e = (s, o, t) => (T(s, o, "read from private field"), t ? t.call(s) : o.get(s)), a = (s, o, t) => {
  if (o.has(s))
    throw TypeError("Cannot add the same private member more than once");
  o instanceof WeakSet ? o.add(s) : o.set(s, t);
}, n = (s, o, t, i) => (T(s, o, "write to private field"), i ? i.call(s, t) : o.set(s, t), t);
import { UmbBooleanState as d, UmbArrayState as A } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken as g } from "@umbraco-cms/backoffice/context-api";
import { u as C, O } from "./index-B75q-oTb.js";
import "@umbraco-cms/backoffice/resources";
import { UmbBaseController as v } from "@umbraco-cms/backoffice/class-api";
import { UMB_AUTH_CONTEXT as y } from "@umbraco-cms/backoffice/auth";
import "@umbraco-cms/backoffice/external/lit";
var r, h, m, p, l, c;
class E extends v {
  constructor(t) {
    super(t);
    a(this, r, void 0);
    a(this, h, void 0);
    a(this, m, void 0);
    a(this, p, void 0);
    a(this, l, void 0);
    a(this, c, void 0);
    n(this, h, new d(!1)), this.loaded = e(this, h).asObservable(), n(this, m, new A([], (i) => i.key)), this.actions = e(this, m).asObservable(), n(this, p, new A([], (i) => i.actionName)), this.currentAction = e(this, p).asObservable(), n(this, l, new d(!1)), this.working = e(this, l).asObservable(), n(this, c, new d(!1)), this.completed = e(this, c).asObservable(), this.provideContext(N, this), n(this, r, new C(this)), this.consumeContext(y, (i) => {
      O.TOKEN = () => i.getLatestToken(), O.WITH_CREDENTIALS = !0, e(this, h).setValue(!0);
    });
  }
  async getActions() {
    const { data: t } = await e(this, r).getActions();
    t && e(this, m).setValue(t);
  }
  async getTime() {
    const { data: t } = await e(this, r).getTime();
    t && console.log(t);
  }
  async performAction(t, i) {
    console.log("Perform Action:", t, i), e(this, l).setValue(!0), e(this, c).setValue(!1);
    var f = !1, b = "", w = 0;
    do {
      const { data: u } = await e(this, r).performAction(b, t, i, w);
      u ? (w++, console.log(u), e(this, p).setValue(u.actionInfo), b = u.requestId, f = u.completed) : f = !0;
    } while (!f);
    e(this, c).setValue(!0), e(this, l).setValue(!1);
  }
}
r = new WeakMap(), h = new WeakMap(), m = new WeakMap(), p = new WeakMap(), l = new WeakMap(), c = new WeakMap();
const N = new g(E.name);
export {
  N as USYNC_CORE_CONTEXT_TOKEN,
  E as default,
  E as uSyncWorkspaceContext
};
//# sourceMappingURL=workspace.context-nF2oOeUu.js.map
