var q = (e, t, n) => {
  if (!t.has(e))
    throw TypeError("Cannot " + n);
};
var i = (e, t, n) => (q(e, t, "read from private field"), n ? n.call(e) : t.get(e)), p = (e, t, n) => {
  if (t.has(e))
    throw TypeError("Cannot add the same private member more than once");
  t instanceof WeakSet ? t.add(e) : t.set(e, n);
}, m = (e, t, n, r) => (q(e, t, "write to private field"), r ? r.call(e, n) : t.set(e, n), n);
import { tryExecuteAndNotify as O } from "@umbraco-cms/backoffice/resources";
import { UmbBaseController as K } from "@umbraco-cms/backoffice/class-api";
import { css as F, property as j, customElement as W, LitElement as L, html as $, nothing as Q } from "@umbraco-cms/backoffice/external/lit";
class V extends Error {
  constructor(t, n, r) {
    super(r), this.name = "ApiError", this.url = n.url, this.status = n.status, this.statusText = n.statusText, this.body = n.body, this.request = t;
  }
}
class X extends Error {
  constructor(t) {
    super(t), this.name = "CancelError";
  }
  get isCancelled() {
    return !0;
  }
}
var d, h, y, f, g, T, x;
class Y {
  constructor(t) {
    p(this, d, void 0);
    p(this, h, void 0);
    p(this, y, void 0);
    p(this, f, void 0);
    p(this, g, void 0);
    p(this, T, void 0);
    p(this, x, void 0);
    m(this, d, !1), m(this, h, !1), m(this, y, !1), m(this, f, []), m(this, g, new Promise((n, r) => {
      m(this, T, n), m(this, x, r);
      const s = (u) => {
        var l;
        i(this, d) || i(this, h) || i(this, y) || (m(this, d, !0), (l = i(this, T)) == null || l.call(this, u));
      }, o = (u) => {
        var l;
        i(this, d) || i(this, h) || i(this, y) || (m(this, h, !0), (l = i(this, x)) == null || l.call(this, u));
      }, a = (u) => {
        i(this, d) || i(this, h) || i(this, y) || i(this, f).push(u);
      };
      return Object.defineProperty(a, "isResolved", {
        get: () => i(this, d)
      }), Object.defineProperty(a, "isRejected", {
        get: () => i(this, h)
      }), Object.defineProperty(a, "isCancelled", {
        get: () => i(this, y)
      }), t(s, o, a);
    }));
  }
  get [Symbol.toStringTag]() {
    return "Cancellable Promise";
  }
  then(t, n) {
    return i(this, g).then(t, n);
  }
  catch(t) {
    return i(this, g).catch(t);
  }
  finally(t) {
    return i(this, g).finally(t);
  }
  cancel() {
    var t;
    if (!(i(this, d) || i(this, h) || i(this, y))) {
      if (m(this, y, !0), i(this, f).length)
        try {
          for (const n of i(this, f))
            n();
        } catch (n) {
          console.warn("Cancellation threw an error", n);
          return;
        }
      i(this, f).length = 0, (t = i(this, x)) == null || t.call(this, new X("Request aborted"));
    }
  }
  get isCancelled() {
    return i(this, y);
  }
}
d = new WeakMap(), h = new WeakMap(), y = new WeakMap(), f = new WeakMap(), g = new WeakMap(), T = new WeakMap(), x = new WeakMap();
const N = {
  BASE: "",
  VERSION: "Latest",
  WITH_CREDENTIALS: !1,
  CREDENTIALS: "include",
  TOKEN: void 0,
  USERNAME: void 0,
  PASSWORD: void 0,
  HEADERS: void 0,
  ENCODE_PATH: void 0
}, _ = (e) => e != null, C = (e) => typeof e == "string", v = (e) => C(e) && e !== "", B = (e) => typeof e == "object" && typeof e.type == "string" && typeof e.stream == "function" && typeof e.arrayBuffer == "function" && typeof e.constructor == "function" && typeof e.constructor.name == "string" && /^(Blob|File)$/.test(e.constructor.name) && /^(Blob|File)$/.test(e[Symbol.toStringTag]), J = (e) => e instanceof FormData, Z = (e) => {
  try {
    return btoa(e);
  } catch {
    return Buffer.from(e).toString("base64");
  }
}, tt = (e) => {
  const t = [], n = (s, o) => {
    t.push(`${encodeURIComponent(s)}=${encodeURIComponent(String(o))}`);
  }, r = (s, o) => {
    _(o) && (Array.isArray(o) ? o.forEach((a) => {
      r(s, a);
    }) : typeof o == "object" ? Object.entries(o).forEach(([a, u]) => {
      r(`${s}[${a}]`, u);
    }) : n(s, o));
  };
  return Object.entries(e).forEach(([s, o]) => {
    r(s, o);
  }), t.length > 0 ? `?${t.join("&")}` : "";
}, et = (e, t) => {
  const n = e.ENCODE_PATH || encodeURI, r = t.url.replace("{api-version}", e.VERSION).replace(/{(.*?)}/g, (o, a) => {
    var u;
    return (u = t.path) != null && u.hasOwnProperty(a) ? n(String(t.path[a])) : o;
  }), s = `${e.BASE}${r}`;
  return t.query ? `${s}${tt(t.query)}` : s;
}, nt = (e) => {
  if (e.formData) {
    const t = new FormData(), n = (r, s) => {
      C(s) || B(s) ? t.append(r, s) : t.append(r, JSON.stringify(s));
    };
    return Object.entries(e.formData).filter(([r, s]) => _(s)).forEach(([r, s]) => {
      Array.isArray(s) ? s.forEach((o) => n(r, o)) : n(r, s);
    }), t;
  }
}, k = async (e, t) => typeof t == "function" ? t(e) : t, st = async (e, t) => {
  const n = await k(t, e.TOKEN), r = await k(t, e.USERNAME), s = await k(t, e.PASSWORD), o = await k(t, e.HEADERS), a = Object.entries({
    Accept: "application/json",
    ...o,
    ...t.headers
  }).filter(([u, l]) => _(l)).reduce((u, [l, b]) => ({
    ...u,
    [l]: String(b)
  }), {});
  if (v(n) && (a.Authorization = `Bearer ${n}`), v(r) && v(s)) {
    const u = Z(`${r}:${s}`);
    a.Authorization = `Basic ${u}`;
  }
  return t.body && (t.mediaType ? a["Content-Type"] = t.mediaType : B(t.body) ? a["Content-Type"] = t.body.type || "application/octet-stream" : C(t.body) ? a["Content-Type"] = "text/plain" : J(t.body) || (a["Content-Type"] = "application/json")), new Headers(a);
}, rt = (e) => {
  var t;
  if (e.body !== void 0)
    return (t = e.mediaType) != null && t.includes("/json") ? JSON.stringify(e.body) : C(e.body) || B(e.body) || J(e.body) ? e.body : JSON.stringify(e.body);
}, ot = async (e, t, n, r, s, o, a) => {
  const u = new AbortController(), l = {
    headers: o,
    body: r ?? s,
    method: t.method,
    signal: u.signal
  };
  return e.WITH_CREDENTIALS && (l.credentials = e.CREDENTIALS), a(() => u.abort()), await fetch(n, l);
}, at = (e, t) => {
  if (t) {
    const n = e.headers.get(t);
    if (C(n))
      return n;
  }
}, it = async (e) => {
  if (e.status !== 204)
    try {
      const t = e.headers.get("Content-Type");
      if (t)
        return ["application/json", "application/problem+json"].some((s) => t.toLowerCase().startsWith(s)) ? await e.json() : await e.text();
    } catch (t) {
      console.error(t);
    }
}, ct = (e, t) => {
  const r = {
    400: "Bad Request",
    401: "Unauthorized",
    403: "Forbidden",
    404: "Not Found",
    500: "Internal Server Error",
    502: "Bad Gateway",
    503: "Service Unavailable",
    ...e.errors
  }[t.status];
  if (r)
    throw new V(e, t, r);
  if (!t.ok) {
    const s = t.status ?? "unknown", o = t.statusText ?? "unknown", a = (() => {
      try {
        return JSON.stringify(t.body, null, 2);
      } catch {
        return;
      }
    })();
    throw new V(
      e,
      t,
      `Generic Error: status: ${s}; status text: ${o}; body: ${a}`
    );
  }
}, R = (e, t) => new Y(async (n, r, s) => {
  try {
    const o = et(e, t), a = nt(t), u = rt(t), l = await st(e, t);
    if (!s.isCancelled) {
      const b = await ot(e, t, o, u, a, l, s), G = await it(b), M = at(b, t.responseHeader), U = {
        url: o,
        ok: b.ok,
        status: b.status,
        statusText: b.statusText,
        body: M ?? G
      };
      ct(t, U), n(U.body);
    }
  } catch (o) {
    r(o);
  }
});
class D {
  /**
   * @returns any Success
   * @throws ApiError
   */
  static getActions() {
    return R(N, {
      method: "GET",
      url: "/umbraco/usync/api/v1/core/actions"
    });
  }
  /**
   * @returns any Success
   * @throws ApiError
   */
  static performAction({
    requestBody: t
  }) {
    return R(N, {
      method: "POST",
      url: "/umbraco/usync/api/v1/core/Perform",
      body: t,
      mediaType: "application/json"
    });
  }
  /**
   * @returns string Success
   * @throws ApiError
   */
  static getTime() {
    return R(N, {
      method: "GET",
      url: "/umbraco/usync/api/v1/core/time"
    });
  }
}
var w;
class ut {
  constructor(t) {
    p(this, w, void 0);
    m(this, w, t);
  }
  async getActions() {
    return await O(i(this, w), D.getActions());
  }
  async getTime() {
    return await O(i(this, w), D.getTime());
  }
  async performAction(t) {
    return await O(i(this, w), D.performAction({
      requestBody: t
    }));
  }
}
w = new WeakMap();
var S;
class Nt extends K {
  constructor(n) {
    super(n);
    p(this, S, void 0);
    m(this, S, new ut(this));
  }
  async getActions() {
    return i(this, S).getActions();
  }
  async getTime() {
    return i(this, S).getTime();
  }
  async performAction(n, r, s, o) {
    return i(this, S).performAction({
      requestId: n,
      groupName: r,
      actionName: s,
      stepNumber: o
    });
  }
}
S = new WeakMap();
const lt = {
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
}, c = lt, mt = "Umb.Section.Settings", yt = {
  type: "menu",
  alias: c.menu.alias,
  name: c.menu.name,
  meta: {
    label: c.menu.label
  }
}, pt = {
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
      match: mt
    }
  ]
}, dt = {
  type: "tree",
  alias: c.tree.alias,
  name: c.tree.name,
  meta: {
    repositoryAlias: c.tree.respository
  }
}, ht = {
  type: "menuItem",
  alias: c.menu.item.alias,
  name: c.menu.item.name,
  meta: {
    label: c.name,
    icon: c.icon,
    entityType: c.workspace.rootElement,
    menus: [c.menu.alias]
  }
}, ft = [yt, pt, ht, dt], P = c.workspace.alias, bt = {
  type: "workspaceContext",
  alias: c.workspace.contextAlias,
  name: "uSync workspace context",
  js: () => import("./workspace.context-nF2oOeUu.js")
}, gt = {
  type: "workspace",
  alias: P,
  name: c.workspace.name,
  js: () => import("./workspace.element-yGco6kLx.js"),
  meta: {
    entityType: c.workspace.rootElement
  }
}, wt = [
  {
    type: "workspaceView",
    alias: c.workspace.defaultView.alias,
    name: c.workspace.defaultView.name,
    js: () => import("./default.element-GS7odW1V.js"),
    weight: 300,
    meta: {
      label: "Default",
      pathname: "default",
      icon: "icon-box"
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: P
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
        match: P
      }
    ]
  }
], St = [], z = [bt, gt, ...wt, ...St];
var xt = Object.defineProperty, At = Object.getOwnPropertyDescriptor, I = (e, t, n, r) => {
  for (var s = r > 1 ? void 0 : r ? At(t, n) : t, o = e.length - 1, a; o >= 0; o--)
    (a = e[o]) && (s = (r ? a(t, n, s) : a(s)) || s);
  return r && s && xt(t, n, s), s;
};
let A = class extends L {
  constructor() {
    super(...arguments), this.myName = "";
  }
  _handleClick(e, t) {
    this.dispatchEvent(new CustomEvent("perform-action", {
      detail: {
        group: e,
        key: t.key
      }
    }));
  }
  render() {
    var n, r, s, o;
    const e = (n = this.group) == null ? void 0 : n.key, t = (r = this.group) == null ? void 0 : r.buttons.map((a) => $`
                <uui-button label=${a.key} 
                    color=${a.color}
                    look=${a.look}
                    style="font-size: 20px"
                    @click=${() => this._handleClick(e, a)}
                    ></uui-button>
            `);
    return $`
                <uui-box class='action-box'>

                    <div class="box-content">

                        <h2 class="box-heading">${(s = this.group) == null ? void 0 : s.groupName}</h2>

                        <uui-icon name=${(o = this.group) == null ? void 0 : o.icon}></uui-icon>
                    
                        <div class="box-buttons">
                            ${t}
                        </div>
                        
                    </div>
                </uui-box>
        `;
  }
};
A.styles = F`

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
I([
  j({ type: String })
], A.prototype, "myName", 2);
I([
  j({ type: Object })
], A.prototype, "group", 2);
A = I([
  W("usync-action-box")
], A);
var Et = Object.defineProperty, Tt = Object.getOwnPropertyDescriptor, H = (e, t, n, r) => {
  for (var s = r > 1 ? void 0 : r ? Tt(t, n) : t, o = e.length - 1, a; o >= 0; o--)
    (a = e[o]) && (s = (r ? a(t, n, s) : a(s)) || s);
  return r && s && Et(t, n, s), s;
};
let E = class extends L {
  constructor() {
    super(...arguments), this.title = "";
  }
  render() {
    var t, n;
    if (console.log("progress box", (t = this.actions) == null ? void 0 : t.length), !this.actions)
      return Q;
    var e = (n = this.actions) == null ? void 0 : n.map((r) => $`
                <div class="action 
                    ${r.completed ? "complete" : ""} ${r.working ? "working" : ""}">
                    <uui-icon .name=${r.icon}></uui-icon>
                    <h4>${r.actionName}</h4>
                </div>
            `);
    return $`
            <uui-box>
                <h2>${this.title}</h2>
                <div class="action-list">
                    ${e}
                </div>
            </uui-box>
        `;
  }
};
E.styles = F`
        uui-box {
            margin: var(--uui-size-layout-1);
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
H([
  j({ type: String })
], E.prototype, "title", 2);
H([
  j({ type: Array })
], E.prototype, "actions", 2);
E = H([
  W("usync-progress-box")
], E);
const Ct = [
  {
    type: "globalContext",
    alias: "uSync.GlobalContext.Actions",
    name: "uSync Action Context",
    js: () => import("./workspace.context-nF2oOeUu.js")
  }
], vt = (e, t) => {
  console.log(z), t.registerMany(
    [
      ...Ct,
      ...ft,
      ...z
    ]
  );
};
export {
  N as O,
  ut as a,
  vt as o,
  Nt as u
};
//# sourceMappingURL=index-B75q-oTb.js.map
