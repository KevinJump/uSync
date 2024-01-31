import { UmbElementMixin as b } from "@umbraco-cms/backoffice/element-api";
import { LitElement as A, html as c, css as E, property as a, customElement as P, nothing as _ } from "@umbraco-cms/backoffice/external/lit";
import { USYNC_CORE_CONTEXT_TOKEN as S } from "./workspace.context-nF2oOeUu.js";
import "@umbraco-cms/backoffice/observable-api";
import "@umbraco-cms/backoffice/context-api";
import "./index-B75q-oTb.js";
import "@umbraco-cms/backoffice/resources";
import "@umbraco-cms/backoffice/class-api";
import "@umbraco-cms/backoffice/auth";
var $ = Object.defineProperty, O = Object.getOwnPropertyDescriptor, i = (t, e, o, s) => {
  for (var n = s > 1 ? void 0 : s ? O(e, o) : e, d = t.length - 1, f; d >= 0; d--)
    (f = t[d]) && (n = (s ? f(e, o, n) : f(n)) || n);
  return s && n && $(e, o, n), n;
}, w = (t, e, o) => {
  if (!e.has(t))
    throw TypeError("Cannot " + o);
}, h = (t, e, o) => (w(t, e, "read from private field"), o ? o.call(t) : e.get(t)), l = (t, e, o) => {
  if (e.has(t))
    throw TypeError("Cannot add the same private member more than once");
  e instanceof WeakSet ? e.add(t) : e.set(t, o);
}, B = (t, e, o, s) => (w(t, e, "write to private field"), s ? s.call(t, o) : e.set(t, o), o), m = (t, e, o) => (w(t, e, "access private method"), o), p, u, g, x, y, k, v, C;
let r = class extends b(A) {
  constructor() {
    super(), l(this, g), l(this, y), l(this, v), l(this, p, void 0), l(this, u, !1), this.loaded = !1, this.working = !1, this.completed = !1, this.showProgress = !1, this.group = "", console.log("element constructor");
  }
  connectedCallback() {
    super.connectedCallback(), console.log("connected"), m(this, g, x).call(this);
  }
  /**
   * 
   * @param event 
   */
  performAction(t) {
    var e;
    this.showProgress = !0, console.log(t.detail), this.group = t.detail.group, (e = h(this, p)) == null || e.performAction(t.detail.group, t.detail.key);
  }
  render() {
    var e, o;
    if (this.loaded == !1)
      return c`<uui-loader></uui-loader>`;
    console.log("element actions", (e = this.actions) == null ? void 0 : e.length);
    var t = (o = this.actions) == null ? void 0 : o.map((s) => c`
                <usync-action-box myName="fred"
                    .group="${s}"
                    @perform-action=${this.performAction}>
                </usync-action-box>
            `);
    return c`
                <div class="action-buttons-box">
                    ${t}
                </div>

                ${m(this, y, k).call(this)}

                ${m(this, v, C).call(this)}
            `;
  }
};
p = /* @__PURE__ */ new WeakMap();
u = /* @__PURE__ */ new WeakMap();
g = /* @__PURE__ */ new WeakSet();
x = function() {
  this.consumeContext(S, (t) => {
    console.log("consume context"), B(this, p, t), this.observe(t.actions, (e) => {
      this.actions = e, this.loaded = this.actions !== null;
    }), this.observe(t.currentAction, (e) => {
      this.workingActions = e;
    }), this.observe(t.working, (e) => {
      this.working = e;
    }), this.observe(t.completed, (e) => {
      this.completed = e;
    }), this.observe(t.loaded, (e) => {
      var o;
      e && h(this, u) == !1 && ((o = h(this, p)) == null || o.getActions(), h(this, u));
    });
  });
};
y = /* @__PURE__ */ new WeakSet();
k = function() {
  var t;
  return this.showProgress == !1 ? _ : (console.log("element working actions", (t = this.workingActions) == null ? void 0 : t.length), c`
            <usync-progress-box .title=${this.group}
                .actions=${this.workingActions}></usync-progress-box>
        `);
};
v = /* @__PURE__ */ new WeakSet();
C = function() {
  return this.completed == !1 ? _ : c`
            <uui-box>
                <h2>Report Here</h2>
            </uui-box>
        `;
};
r.styles = [
  E`
            usync-action-box, uui-box {
               margin: var(--uui-size-layout-1);
            }

            .action-buttons-box {
               display: grid;
               grid-template-columns: 1fr 1fr 1fr;
            }        
        `
];
i([
  a({ type: Array })
], r.prototype, "actions", 2);
i([
  a({ type: Boolean })
], r.prototype, "loaded", 2);
i([
  a({ type: Array })
], r.prototype, "workingActions", 2);
i([
  a({ type: Boolean })
], r.prototype, "working", 2);
i([
  a({ type: Boolean })
], r.prototype, "completed", 2);
i([
  a({ type: Boolean })
], r.prototype, "showProgress", 2);
i([
  a({ type: String })
], r.prototype, "group", 2);
r = i([
  P("usync-default-view")
], r);
const z = r;
export {
  z as default,
  r as uSyncDefaultViewElement
};
//# sourceMappingURL=default.element-GS7odW1V.js.map
