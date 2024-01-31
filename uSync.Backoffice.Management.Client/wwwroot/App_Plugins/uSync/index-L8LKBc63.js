var H = (t, e, s) => {
  if (!e.has(t))
    throw TypeError("Cannot " + s);
};
var i = (t, e, s) => (H(t, e, "read from private field"), s ? s.call(t) : e.get(t)), m = (t, e, s) => {
  if (e.has(t))
    throw TypeError("Cannot add the same private member more than once");
  e instanceof WeakSet ? e.add(t) : e.set(t, s);
}, p = (t, e, s, r) => (H(t, e, "write to private field"), r ? r.call(t, s) : e.set(t, s), s);
import { tryExecuteAndNotify as L } from "@umbraco-cms/backoffice/resources";
import { UmbBaseController as Z } from "@umbraco-cms/backoffice/class-api";
import { css as N, property as O, customElement as D, LitElement as _, html as E, nothing as M } from "@umbraco-cms/backoffice/external/lit";
class z extends Error {
  constructor(e, s, r) {
    super(r), this.name = "ApiError", this.url = s.url, this.status = s.status, this.statusText = s.statusText, this.body = s.body, this.request = e;
  }
}
class ee extends Error {
  constructor(e) {
    super(e), this.name = "CancelError";
  }
  get isCancelled() {
    return !0;
  }
}
var d, h, y, b, w, $, g;
class te {
  constructor(e) {
    m(this, d, void 0);
    m(this, h, void 0);
    m(this, y, void 0);
    m(this, b, void 0);
    m(this, w, void 0);
    m(this, $, void 0);
    m(this, g, void 0);
    p(this, d, !1), p(this, h, !1), p(this, y, !1), p(this, b, []), p(this, w, new Promise((s, r) => {
      p(this, $, s), p(this, g, r);
      const n = (u) => {
        var l;
        i(this, d) || i(this, h) || i(this, y) || (p(this, d, !0), (l = i(this, $)) == null || l.call(this, u));
      }, o = (u) => {
        var l;
        i(this, d) || i(this, h) || i(this, y) || (p(this, h, !0), (l = i(this, g)) == null || l.call(this, u));
      }, a = (u) => {
        i(this, d) || i(this, h) || i(this, y) || i(this, b).push(u);
      };
      return Object.defineProperty(a, "isResolved", {
        get: () => i(this, d)
      }), Object.defineProperty(a, "isRejected", {
        get: () => i(this, h)
      }), Object.defineProperty(a, "isCancelled", {
        get: () => i(this, y)
      }), e(n, o, a);
    }));
  }
  get [Symbol.toStringTag]() {
    return "Cancellable Promise";
  }
  then(e, s) {
    return i(this, w).then(e, s);
  }
  catch(e) {
    return i(this, w).catch(e);
  }
  finally(e) {
    return i(this, w).finally(e);
  }
  cancel() {
    var e;
    if (!(i(this, d) || i(this, h) || i(this, y))) {
      if (p(this, y, !0), i(this, b).length)
        try {
          for (const s of i(this, b))
            s();
        } catch (s) {
          console.warn("Cancellation threw an error", s);
          return;
        }
      i(this, b).length = 0, (e = i(this, g)) == null || e.call(this, new ee("Request aborted"));
    }
  }
  get isCancelled() {
    return i(this, y);
  }
}
d = new WeakMap(), h = new WeakMap(), y = new WeakMap(), b = new WeakMap(), w = new WeakMap(), $ = new WeakMap(), g = new WeakMap();
const F = {
  BASE: "",
  VERSION: "Latest",
  WITH_CREDENTIALS: !1,
  CREDENTIALS: "include",
  TOKEN: void 0,
  USERNAME: void 0,
  PASSWORD: void 0,
  HEADERS: void 0,
  ENCODE_PATH: void 0
};
var P = /* @__PURE__ */ ((t) => (t.PENDING = "Pending", t.PROCESSING = "Processing", t.COMPLETE = "Complete", t.ERROR = "Error", t))(P || {});
const I = (t) => t != null, T = (t) => typeof t == "string", R = (t) => T(t) && t !== "", B = (t) => typeof t == "object" && typeof t.type == "string" && typeof t.stream == "function" && typeof t.arrayBuffer == "function" && typeof t.constructor == "function" && typeof t.constructor.name == "string" && /^(Blob|File)$/.test(t.constructor.name) && /^(Blob|File)$/.test(t[Symbol.toStringTag]), K = (t) => t instanceof FormData, se = (t) => {
  try {
    return btoa(t);
  } catch {
    return Buffer.from(t).toString("base64");
  }
}, ne = (t) => {
  const e = [], s = (n, o) => {
    e.push(`${encodeURIComponent(n)}=${encodeURIComponent(String(o))}`);
  }, r = (n, o) => {
    I(o) && (Array.isArray(o) ? o.forEach((a) => {
      r(n, a);
    }) : typeof o == "object" ? Object.entries(o).forEach(([a, u]) => {
      r(`${n}[${a}]`, u);
    }) : s(n, o));
  };
  return Object.entries(t).forEach(([n, o]) => {
    r(n, o);
  }), e.length > 0 ? `?${e.join("&")}` : "";
}, re = (t, e) => {
  const s = t.ENCODE_PATH || encodeURI, r = e.url.replace("{api-version}", t.VERSION).replace(/{(.*?)}/g, (o, a) => {
    var u;
    return (u = e.path) != null && u.hasOwnProperty(a) ? s(String(e.path[a])) : o;
  }), n = `${t.BASE}${r}`;
  return e.query ? `${n}${ne(e.query)}` : n;
}, oe = (t) => {
  if (t.formData) {
    const e = new FormData(), s = (r, n) => {
      T(n) || B(n) ? e.append(r, n) : e.append(r, JSON.stringify(n));
    };
    return Object.entries(t.formData).filter(([r, n]) => I(n)).forEach(([r, n]) => {
      Array.isArray(n) ? n.forEach((o) => s(r, o)) : s(r, n);
    }), e;
  }
}, v = async (t, e) => typeof e == "function" ? e(t) : e, ae = async (t, e) => {
  const s = await v(e, t.TOKEN), r = await v(e, t.USERNAME), n = await v(e, t.PASSWORD), o = await v(e, t.HEADERS), a = Object.entries({
    Accept: "application/json",
    ...o,
    ...e.headers
  }).filter(([u, l]) => I(l)).reduce((u, [l, f]) => ({
    ...u,
    [l]: String(f)
  }), {});
  if (R(s) && (a.Authorization = `Bearer ${s}`), R(r) && R(n)) {
    const u = se(`${r}:${n}`);
    a.Authorization = `Basic ${u}`;
  }
  return e.body && (e.mediaType ? a["Content-Type"] = e.mediaType : B(e.body) ? a["Content-Type"] = e.body.type || "application/octet-stream" : T(e.body) ? a["Content-Type"] = "text/plain" : K(e.body) || (a["Content-Type"] = "application/json")), new Headers(a);
}, ie = (t) => {
  var e;
  if (t.body !== void 0)
    return (e = t.mediaType) != null && e.includes("/json") ? JSON.stringify(t.body) : T(t.body) || B(t.body) || K(t.body) ? t.body : JSON.stringify(t.body);
}, ce = async (t, e, s, r, n, o, a) => {
  const u = new AbortController(), l = {
    headers: o,
    body: r ?? n,
    method: e.method,
    signal: u.signal
  };
  return t.WITH_CREDENTIALS && (l.credentials = t.CREDENTIALS), a(() => u.abort()), await fetch(s, l);
}, ue = (t, e) => {
  if (e) {
    const s = t.headers.get(e);
    if (T(s))
      return s;
  }
}, le = async (t) => {
  if (t.status !== 204)
    try {
      const e = t.headers.get("Content-Type");
      if (e)
        return ["application/json", "application/problem+json"].some((n) => e.toLowerCase().startsWith(n)) ? await t.json() : await t.text();
    } catch (e) {
      console.error(e);
    }
}, pe = (t, e) => {
  const r = {
    400: "Bad Request",
    401: "Unauthorized",
    403: "Forbidden",
    404: "Not Found",
    500: "Internal Server Error",
    502: "Bad Gateway",
    503: "Service Unavailable",
    ...t.errors
  }[e.status];
  if (r)
    throw new z(t, e, r);
  if (!e.ok) {
    const n = e.status ?? "unknown", o = e.statusText ?? "unknown", a = (() => {
      try {
        return JSON.stringify(e.body, null, 2);
      } catch {
        return;
      }
    })();
    throw new z(
      t,
      e,
      `Generic Error: status: ${n}; status text: ${o}; body: ${a}`
    );
  }
}, W = (t, e) => new te(async (s, r, n) => {
  try {
    const o = re(t, e), a = oe(e), u = ie(e), l = await ae(t, e);
    if (!n.isCancelled) {
      const f = await ce(t, e, o, u, a, l, n), X = await le(f), Y = ue(f, e.responseHeader), q = {
        url: o,
        ok: f.ok,
        status: f.status,
        statusText: f.statusText,
        body: Y ?? X
      };
      pe(e, q), s(q.body);
    }
  } catch (o) {
    r(o);
  }
});
class G {
  /**
   * @returns any Success
   * @throws ApiError
   */
  static getActions() {
    return W(F, {
      method: "GET",
      url: "/umbraco/usync/api/v1/Actions"
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  static performAction({
    requestBody: e
  }) {
    return W(F, {
      method: "POST",
      url: "/umbraco/usync/api/v1/Perform",
      body: e,
      mediaType: "application/json"
    });
  }
}
var S;
class ye {
  constructor(e) {
    m(this, S, void 0);
    p(this, S, e);
  }
  async getActions() {
    return await L(i(this, S), G.getActions());
  }
  async performAction(e) {
    return await L(i(this, S), G.performAction({
      requestBody: e
    }));
  }
}
S = new WeakMap();
var x;
class _e extends Z {
  constructor(s) {
    super(s);
    m(this, x, void 0);
    p(this, x, new ye(this));
  }
  async getActions() {
    return i(this, x).getActions();
  }
  async performAction(s, r, n, o) {
    return i(this, x).performAction(
      {
        requestId: s,
        action: n,
        options: {
          group: r,
          force: !0,
          clean: !1
        },
        stepNumber: o
      }
    );
  }
}
x = new WeakMap();
const me = {
  name: "uSync",
  path: "usync",
  icon: "icon-infinity",
  menuName: "Syncronisation",
  workspace: {
    alias: "usync.workspace",
    name: "uSync root workspace",
    rootElement: "usync-root",
    elementName: "usync-workspace-root",
    contextAlias: "usync.workspace.context",
    defaultView: {
      alias: "usync.workspace.default",
      name: "uSync workspace default view",
      icon: "icon-infinity",
      path: "usync.workspace.default"
    },
    settingView: {
      alias: "usync.workspace.settings",
      name: "uSync workspace settings view",
      icon: "icon-settings",
      path: "usync.workspace.settings"
    }
  },
  dashboard: {
    name: "uSyncDashboard",
    alias: "usync.dashboard",
    elementName: "usync-dashboard",
    path: "usync.dashboard",
    weight: -10,
    section: "Umb.Section.Settings"
  },
  tree: {
    name: "uSyncTree",
    alias: "usync.tree",
    respository: ""
  },
  menu: {
    sidebar: "usync.sidebar",
    alias: "usync.menu",
    name: "usync.Menu",
    label: "Syncronisation",
    item: {
      alias: "usync.menu.item",
      name: "usync.menu.item"
    }
  }
}, c = me, de = "Umb.Section.Settings", he = {
  type: "menu",
  alias: c.menu.alias,
  name: c.menu.name,
  meta: {
    label: c.menu.label
  }
}, be = {
  type: "sectionSidebarApp",
  kind: "menu",
  alias: c.menu.sidebar,
  name: "uSync section sidebar menu",
  weight: 150,
  meta: {
    label: c.menu.label,
    menu: c.menu.alias
  },
  conditions: [
    {
      alias: "Umb.Condition.SectionAlias",
      match: de
    }
  ]
}, fe = {
  type: "tree",
  alias: c.tree.alias,
  name: c.tree.name,
  meta: {
    repositoryAlias: c.tree.respository
  }
}, we = {
  type: "menuItem",
  alias: c.menu.item.alias,
  name: c.menu.item.name,
  meta: {
    label: c.name,
    icon: c.icon,
    entityType: c.workspace.rootElement,
    menus: [c.menu.alias]
  }
}, ge = [he, be, we, fe], k = c.workspace.alias, Se = {
  type: "workspaceContext",
  alias: c.workspace.contextAlias,
  name: "uSync workspace context",
  js: () => import("./workspace.context-tRLO8UV-.js")
}, xe = {
  type: "workspace",
  alias: k,
  name: c.workspace.name,
  js: () => import("./workspace.element-xWfSL1Tl.js"),
  meta: {
    entityType: c.workspace.rootElement
  }
}, Ee = [
  {
    type: "workspaceView",
    alias: c.workspace.defaultView.alias,
    name: c.workspace.defaultView.name,
    js: () => import("./default.element-M-mc6NXl.js"),
    weight: 300,
    meta: {
      label: "Default",
      pathname: "default",
      icon: "icon-box"
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: k
      }
    ]
  },
  {
    type: "workspaceView",
    alias: c.workspace.settingView.alias,
    name: c.workspace.settingView.name,
    js: () => import("./settings.element-1w1sEdf4.js"),
    weight: 200,
    meta: {
      label: "Settings",
      pathname: "settings",
      icon: "icon-box"
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: k
      }
    ]
  }
], Ae = [], J = [Se, xe, ...Ee, ...Ae];
var Ce = Object.defineProperty, $e = Object.getOwnPropertyDescriptor, U = (t, e, s, r) => {
  for (var n = r > 1 ? void 0 : r ? $e(e, s) : e, o = t.length - 1, a; o >= 0; o--)
    (a = t[o]) && (n = (r ? a(e, s, n) : a(n)) || n);
  return r && n && Ce(e, s, n), n;
};
let A = class extends _ {
  constructor() {
    super(...arguments), this.myName = "";
  }
  _handleClick(t, e) {
    this.dispatchEvent(new CustomEvent("perform-action", {
      detail: {
        group: t,
        key: e.key
      }
    }));
  }
  render() {
    var s, r, n, o;
    const t = (s = this.group) == null ? void 0 : s.key, e = (r = this.group) == null ? void 0 : r.buttons.map((a) => E`
                <uui-button label=${a.key} 
                    color=${a.color}
                    look=${a.look}
                    style="font-size: 20px"
                    @click=${() => this._handleClick(t, a)}
                    ></uui-button>
            `);
    return E`
                <uui-box class='action-box'>

                    <div class="box-content">

                        <h2 class="box-heading">${(n = this.group) == null ? void 0 : n.groupName}</h2>

                        <uui-icon name=${(o = this.group) == null ? void 0 : o.icon}></uui-icon>
                    
                        <div class="box-buttons">
                            ${e}
                        </div>
                        
                    </div>
                </uui-box>
        `;
  }
};
A.styles = N`

        .box-content {
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .box-heading {
            font-size: 20pt;
        }

        uui-icon {
            margin: 20px;
            font-size: 40pt;
        }

        uui-button {
            margin: 0 5px;
        }

        .box-buttons {
            margin-top: 10px;
        }
        `;
U([
  O({ type: String })
], A.prototype, "myName", 2);
U([
  O({ type: Object })
], A.prototype, "group", 2);
A = U([
  D("usync-action-box")
], A);
var Oe = Object.defineProperty, Te = Object.getOwnPropertyDescriptor, V = (t, e, s, r) => {
  for (var n = r > 1 ? void 0 : r ? Te(e, s) : e, o = t.length - 1, a; o >= 0; o--)
    (a = t[o]) && (n = (r ? a(e, s, n) : a(n)) || n);
  return r && n && Oe(e, s, n), n;
};
let C = class extends _ {
  constructor() {
    super(...arguments), this.title = "";
  }
  render() {
    var e, s;
    if (console.log("progress box", (e = this.actions) == null ? void 0 : e.length), !this.actions)
      return M;
    var t = (s = this.actions) == null ? void 0 : s.map((r) => E`
                <div class="action 
                    ${r.status == P.COMPLETE ? "complete" : ""} 
                    ${r.status == P.PROCESSING ? "working" : ""}">
                    <uui-icon .name=${r.icon ?? "icon-box"}></uui-icon>
                    <h4>${r.name ?? "unknown"}</h4>
                </div>
            `);
    return E`
            <uui-box>
                <h2>${this.title}</h2>
                <div class="action-list">
                    ${t}
                </div>
            </uui-box>
        `;
  }
};
C.styles = N`
        uui-box {
            margin: var(--uui-size-space-4);
        }

        h2 {
            text-align: center;
        }

        .action-list {
            display: flex;
            justify-content: space-around;
        }

        .action {
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .action uui-icon {
            font-size: 30pt;
        }
        
        .complete {
            color: blue;
            opacity: 0.5;
        }

        .working {
            color: green;
        }
    `;
V([
  O({ type: String })
], C.prototype, "title", 2);
V([
  O({ type: Array })
], C.prototype, "actions", 2);
C = V([
  D("usync-progress-box")
], C);
var ve = Object.defineProperty, je = Object.getOwnPropertyDescriptor, Q = (t, e, s, r) => {
  for (var n = r > 1 ? void 0 : r ? je(e, s) : e, o = t.length - 1, a; o >= 0; o--)
    (a = t[o]) && (n = (r ? a(e, s, n) : a(n)) || n);
  return r && n && ve(e, s, n), n;
};
let j = class extends _ {
  constructor() {
    super(...arguments), this.results = [];
  }
  render() {
    var t = this.results.map((e) => E`
                <uui-table-row>
                    <uui-table-cell><uui-icon .name=${e.success ? "icon-check" : "icon-wrong"}></uui-icon></uui-table-cell>
                    <uui-table-cell>${e.change}</uui-table-cell>
                    <uui-table-cell>${e.itemType}</uui-table-cell>
                    <uui-table-cell>${e.name}</uui-table-cell>
                    <uui-table-cell>${e.details.length > 0 ? "Show details" : M}</uui-table-cell>
                </uui-table-row>
            `);
    return E`

            <div class="result-header">
                ${this.results.length} items
            </div>

            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Success</uui-table-head-cell>
                    <uui-table-head-cell>Change</uui-table-head-cell>
                    <uui-table-head-cell>Type</uui-table-head-cell>
                    <uui-table-head-cell>Name</uui-table-head-cell>
                    <uui-table-head-cell>Details</uui-table-head-cell>
                </uui-table-head>

                ${t}

            </uui-table>
        `;
  }
};
j.styles = N`
        .result-header {
            display: flex;
            justify-content: flex-end;
        }
    `;
Q([
  O({ type: Array })
], j.prototype, "results", 2);
j = Q([
  D("usync-results")
], j);
const Re = [
  {
    type: "globalContext",
    alias: "uSync.GlobalContext.Actions",
    name: "uSync Action Context",
    js: () => import("./workspace.context-tRLO8UV-.js")
  }
], Ie = (t, e) => {
  console.log(J), e.registerMany(
    [
      ...Re,
      ...ge,
      ...J
    ]
  );
};
export {
  F as O,
  ye as a,
  Ie as o,
  _e as u
};
//# sourceMappingURL=index-L8LKBc63.js.map
