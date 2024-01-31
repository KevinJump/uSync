import { UmbElementMixin as A } from "@umbraco-cms/backoffice/element-api";
import { LitElement as C, html as c, css as E, property as n, customElement as P, nothing as x } from "@umbraco-cms/backoffice/external/lit";
import { USYNC_CORE_CONTEXT_TOKEN as S } from "./workspace.context-tRLO8UV-.js";
import "@umbraco-cms/backoffice/observable-api";
import "@umbraco-cms/backoffice/context-api";
import "./index-L8LKBc63.js";
import "@umbraco-cms/backoffice/resources";
import "@umbraco-cms/backoffice/class-api";
import "@umbraco-cms/backoffice/auth";
var $ = Object.defineProperty, O = Object.getOwnPropertyDescriptor, i = (e, t, o, s) => {
  for (var a = s > 1 ? void 0 : s ? O(t, o) : t, d = e.length - 1, f; d >= 0; d--)
    (f = e[d]) && (a = (s ? f(t, o, a) : f(a)) || a);
  return s && a && $(t, o, a), a;
}, w = (e, t, o) => {
  if (!t.has(e))
    throw TypeError("Cannot " + o);
}, u = (e, t, o) => (w(e, t, "read from private field"), o ? o.call(e) : t.get(e)), l = (e, t, o) => {
  if (t.has(e))
    throw TypeError("Cannot add the same private member more than once");
  t instanceof WeakSet ? t.add(e) : t.set(e, o);
}, B = (e, t, o, s) => (w(e, t, "write to private field"), s ? s.call(e, o) : t.set(e, o), o), m = (e, t, o) => (w(e, t, "access private method"), o), p, h, g, _, y, k, v, b;
let r = class extends A(C) {
  constructor() {
    super(), l(this, g), l(this, y), l(this, v), l(this, p, void 0), l(this, h, !1), this.loaded = !1, this.working = !1, this.completed = !1, this.showProgress = !1, this.group = "", this.results = [], console.log("element constructor");
  }
  connectedCallback() {
    super.connectedCallback(), console.log("connected"), m(this, g, _).call(this);
  }
  /**
   * 
   * @param event 
   */
  performAction(e) {
    var t;
    this.showProgress = !0, console.log(e.detail), this.group = e.detail.group, (t = u(this, p)) == null || t.performAction(e.detail.group, e.detail.key);
  }
  render() {
    var t, o;
    if (this.loaded == !1)
      return c`<uui-loader></uui-loader>`;
    console.log("element actions", (t = this.actions) == null ? void 0 : t.length);
    var e = (o = this.actions) == null ? void 0 : o.map((s) => c`
                <usync-action-box myName="fred"
                    .group="${s}"
                    @perform-action=${this.performAction}>
                </usync-action-box>
            `);
    return c`
                <div class="action-buttons-box">
                    ${e}
                </div>

                ${m(this, y, k).call(this)}

                ${m(this, v, b).call(this)}
            `;
  }
};
p = /* @__PURE__ */ new WeakMap();
h = /* @__PURE__ */ new WeakMap();
g = /* @__PURE__ */ new WeakSet();
_ = function() {
  this.consumeContext(S, (e) => {
    console.log("consume context"), B(this, p, e), this.observe(e.actions, (t) => {
      this.actions = t, this.loaded = this.actions !== null;
    }), this.observe(e.currentAction, (t) => {
      this.workingActions = t;
    }), this.observe(e.working, (t) => {
      this.working = t;
    }), this.observe(e.results, (t) => {
      this.results = t;
    }), this.observe(e.completed, (t) => {
      this.completed = t;
    }), this.observe(e.loaded, (t) => {
      var o;
      t && u(this, h) == !1 && ((o = u(this, p)) == null || o.getActions(), u(this, h));
    });
  });
};
y = /* @__PURE__ */ new WeakSet();
k = function() {
  var e;
  return this.showProgress == !1 ? x : (console.log("element working actions", (e = this.workingActions) == null ? void 0 : e.length), c`
            <usync-progress-box .title=${this.group}
                .actions=${this.workingActions}></usync-progress-box>
        `);
};
v = /* @__PURE__ */ new WeakSet();
b = function() {
  return this.completed == !1 ? x : c`
            <uui-box>
                <usync-results .results=${this.results}></usync-results>
            </uui-box>
        `;
};
r.styles = [
  E`
            usync-action-box, uui-box {
               margin: var(--uui-size-space-4);
            }

            .action-buttons-box {
               display: grid;
               grid-template-columns: 1fr 1fr 1fr;
            }        
        `
];
i([
  n({ type: Array })
], r.prototype, "actions", 2);
i([
  n({ type: Boolean })
], r.prototype, "loaded", 2);
i([
  n({ type: Array })
], r.prototype, "workingActions", 2);
i([
  n({ type: Boolean })
], r.prototype, "working", 2);
i([
  n({ type: Boolean })
], r.prototype, "completed", 2);
i([
  n({ type: Boolean })
], r.prototype, "showProgress", 2);
i([
  n({ type: String })
], r.prototype, "group", 2);
i([
  n({ type: Array })
], r.prototype, "results", 2);
r = i([
  P("usync-default-view")
], r);
const z = r;
export {
  z as default,
  r as uSyncDefaultViewElement
};
//# sourceMappingURL=default.element-M-mc6NXl.js.map
