var U = (t, e, n) => {
  if (!e.has(t))
    throw TypeError("Cannot " + n);
};
var r = (t, e, n) => (U(t, e, "read from private field"), n ? n.call(t) : e.get(t)), y = (t, e, n) => {
  if (e.has(t))
    throw TypeError("Cannot add the same private member more than once");
  e instanceof WeakSet ? e.add(t) : e.set(t, n);
}, m = (t, e, n, a) => (U(t, e, "write to private field"), a ? a.call(t, n) : e.set(t, n), n);
import { tryExecuteAndNotify as x } from "@umbraco-cms/backoffice/resources";
import { UmbBaseController as M } from "@umbraco-cms/backoffice/class-api";
class $ extends Error {
  constructor(e, n, a) {
    super(a), this.name = "ApiError", this.url = n.url, this.status = n.status, this.statusText = n.statusText, this.body = n.body, this.request = e;
  }
}
class W extends Error {
  constructor(e) {
    super(e), this.name = "CancelError";
  }
  get isCancelled() {
    return !0;
  }
}
var h, p, d, f, w, E, S;
class _ {
  constructor(e) {
    y(this, h, void 0);
    y(this, p, void 0);
    y(this, d, void 0);
    y(this, f, void 0);
    y(this, w, void 0);
    y(this, E, void 0);
    y(this, S, void 0);
    m(this, h, !1), m(this, p, !1), m(this, d, !1), m(this, f, []), m(this, w, new Promise((n, a) => {
      m(this, E, n), m(this, S, a);
      const s = (u) => {
        var l;
        r(this, h) || r(this, p) || r(this, d) || (m(this, h, !0), (l = r(this, E)) == null || l.call(this, u));
      }, o = (u) => {
        var l;
        r(this, h) || r(this, p) || r(this, d) || (m(this, p, !0), (l = r(this, S)) == null || l.call(this, u));
      }, i = (u) => {
        r(this, h) || r(this, p) || r(this, d) || r(this, f).push(u);
      };
      return Object.defineProperty(i, "isResolved", {
        get: () => r(this, h)
      }), Object.defineProperty(i, "isRejected", {
        get: () => r(this, p)
      }), Object.defineProperty(i, "isCancelled", {
        get: () => r(this, d)
      }), e(s, o, i);
    }));
  }
  get [Symbol.toStringTag]() {
    return "Cancellable Promise";
  }
  then(e, n) {
    return r(this, w).then(e, n);
  }
  catch(e) {
    return r(this, w).catch(e);
  }
  finally(e) {
    return r(this, w).finally(e);
  }
  cancel() {
    var e;
    if (!(r(this, h) || r(this, p) || r(this, d))) {
      if (m(this, d, !0), r(this, f).length)
        try {
          for (const n of r(this, f))
            n();
        } catch (n) {
          console.warn("Cancellation threw an error", n);
          return;
        }
      r(this, f).length = 0, (e = r(this, S)) == null || e.call(this, new W("Request aborted"));
    }
  }
  get isCancelled() {
    return r(this, d);
  }
}
h = new WeakMap(), p = new WeakMap(), d = new WeakMap(), f = new WeakMap(), w = new WeakMap(), E = new WeakMap(), S = new WeakMap();
const B = {
  BASE: "",
  VERSION: "Latest",
  WITH_CREDENTIALS: !1,
  CREDENTIALS: "include",
  TOKEN: void 0,
  USERNAME: void 0,
  PASSWORD: void 0,
  HEADERS: void 0,
  ENCODE_PATH: void 0
}, D = (t) => t != null, T = (t) => typeof t == "string", R = (t) => T(t) && t !== "", N = (t) => typeof t == "object" && typeof t.type == "string" && typeof t.stream == "function" && typeof t.arrayBuffer == "function" && typeof t.constructor == "function" && typeof t.constructor.name == "string" && /^(Blob|File)$/.test(t.constructor.name) && /^(Blob|File)$/.test(t[Symbol.toStringTag]), P = (t) => t instanceof FormData, J = (t) => {
  try {
    return btoa(t);
  } catch {
    return Buffer.from(t).toString("base64");
  }
}, L = (t) => {
  const e = [], n = (s, o) => {
    e.push(`${encodeURIComponent(s)}=${encodeURIComponent(String(o))}`);
  }, a = (s, o) => {
    D(o) && (Array.isArray(o) ? o.forEach((i) => {
      a(s, i);
    }) : typeof o == "object" ? Object.entries(o).forEach(([i, u]) => {
      a(`${s}[${i}]`, u);
    }) : n(s, o));
  };
  return Object.entries(t).forEach(([s, o]) => {
    a(s, o);
  }), e.length > 0 ? `?${e.join("&")}` : "";
}, G = (t, e) => {
  const n = t.ENCODE_PATH || encodeURI, a = e.url.replace("{api-version}", t.VERSION).replace(/{(.*?)}/g, (o, i) => {
    var u;
    return (u = e.path) != null && u.hasOwnProperty(i) ? n(String(e.path[i])) : o;
  }), s = `${t.BASE}${a}`;
  return e.query ? `${s}${L(e.query)}` : s;
}, v = (t) => {
  if (t.formData) {
    const e = new FormData(), n = (a, s) => {
      T(s) || N(s) ? e.append(a, s) : e.append(a, JSON.stringify(s));
    };
    return Object.entries(t.formData).filter(([a, s]) => D(s)).forEach(([a, s]) => {
      Array.isArray(s) ? s.forEach((o) => n(a, o)) : n(a, s);
    }), e;
  }
}, C = async (t, e) => typeof e == "function" ? e(t) : e, z = async (t, e) => {
  const n = await C(e, t.TOKEN), a = await C(e, t.USERNAME), s = await C(e, t.PASSWORD), o = await C(e, t.HEADERS), i = Object.entries({
    Accept: "application/json",
    ...o,
    ...e.headers
  }).filter(([u, l]) => D(l)).reduce((u, [l, b]) => ({
    ...u,
    [l]: String(b)
  }), {});
  if (R(n) && (i.Authorization = `Bearer ${n}`), R(a) && R(s)) {
    const u = J(`${a}:${s}`);
    i.Authorization = `Basic ${u}`;
  }
  return e.body && (e.mediaType ? i["Content-Type"] = e.mediaType : N(e.body) ? i["Content-Type"] = e.body.type || "application/octet-stream" : T(e.body) ? i["Content-Type"] = "text/plain" : P(e.body) || (i["Content-Type"] = "application/json")), new Headers(i);
}, K = (t) => {
  var e;
  if (t.body !== void 0)
    return (e = t.mediaType) != null && e.includes("/json") ? JSON.stringify(t.body) : T(t.body) || N(t.body) || P(t.body) ? t.body : JSON.stringify(t.body);
}, Q = async (t, e, n, a, s, o, i) => {
  const u = new AbortController(), l = {
    headers: o,
    body: a ?? s,
    method: e.method,
    signal: u.signal
  };
  return t.WITH_CREDENTIALS && (l.credentials = t.CREDENTIALS), i(() => u.abort()), await fetch(n, l);
}, X = (t, e) => {
  if (e) {
    const n = t.headers.get(e);
    if (T(n))
      return n;
  }
}, Y = async (t) => {
  if (t.status !== 204)
    try {
      const e = t.headers.get("Content-Type");
      if (e)
        return ["application/json", "application/problem+json"].some((s) => e.toLowerCase().startsWith(s)) ? await t.json() : await t.text();
    } catch (e) {
      console.error(e);
    }
}, Z = (t, e) => {
  const a = {
    400: "Bad Request",
    401: "Unauthorized",
    403: "Forbidden",
    404: "Not Found",
    500: "Internal Server Error",
    502: "Bad Gateway",
    503: "Service Unavailable",
    ...t.errors
  }[e.status];
  if (a)
    throw new $(t, e, a);
  if (!e.ok) {
    const s = e.status ?? "unknown", o = e.statusText ?? "unknown", i = (() => {
      try {
        return JSON.stringify(e.body, null, 2);
      } catch {
        return;
      }
    })();
    throw new $(
      t,
      e,
      `Generic Error: status: ${s}; status text: ${o}; body: ${i}`
    );
  }
}, I = (t, e) => new _(async (n, a, s) => {
  try {
    const o = G(t, e), i = v(e), u = K(e), l = await z(t, e);
    if (!s.isCancelled) {
      const b = await Q(t, e, o, u, i, l, s), q = await Y(b), F = X(b, e.responseHeader), O = {
        url: o,
        ok: b.ok,
        status: b.status,
        statusText: b.statusText,
        body: F ?? q
      };
      Z(e, O), n(O.body);
    }
  } catch (o) {
    a(o);
  }
});
class V {
  /**
   * @returns any Success
   * @throws ApiError
   */
  static getUmbracoManagementApiV1USyncActions() {
    return I(B, {
      method: "GET",
      url: "/umbraco/management/api/v1/uSync/actions"
    });
  }
  /**
   * @returns string Success
   * @throws ApiError
   */
  static getUmbracoManagementApiV1USyncTime() {
    return I(B, {
      method: "GET",
      url: "/umbraco/management/api/v1/uSync/time"
    });
  }
}
var g;
class ee {
  constructor(e) {
    y(this, g, void 0);
    m(this, g, e);
  }
  async getActions() {
    return await x(r(this, g), V.getUmbracoManagementApiV1USyncActions());
  }
  async getTime() {
    return await x(r(this, g), V.getUmbracoManagementApiV1USyncTime());
  }
}
g = new WeakMap();
var k, A;
class pe extends M {
  constructor(n) {
    super(n);
    y(this, k, void 0);
    y(this, A, void 0);
    m(this, k, n), m(this, A, new ee(this)), console.log("respository init");
  }
  async getActions() {
    return r(this, A).getActions();
  }
  async getTime() {
    return r(this, A).getTime();
  }
}
k = new WeakMap(), A = new WeakMap();
const te = {
  name: "uSync",
  path: "usync",
  icon: "icon-infinity",
  menuName: "Syncronisation",
  workspace: {
    alias: "usync.workspace",
    name: "uSync root workspace",
    rootElement: "usync-root",
    elementName: "usync-workspace-root",
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
}, c = te, ne = "Umb.Section.Settings", se = {
  type: "menu",
  alias: c.menu.alias,
  name: c.menu.name,
  meta: {
    label: c.menu.label
  }
}, re = {
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
      match: ne
    }
  ]
}, ae = {
  type: "tree",
  alias: c.tree.alias,
  name: c.tree.name,
  meta: {
    repositoryAlias: c.tree.respository
  }
}, oe = {
  type: "menuItem",
  alias: c.menu.item.alias,
  name: c.menu.item.name,
  meta: {
    label: c.name,
    icon: c.icon,
    entityType: c.workspace.rootElement,
    menus: [c.menu.alias]
  }
}, ie = [se, re, oe, ae], j = c.workspace.alias, ce = {
  type: "workspace",
  alias: j,
  name: c.workspace.name,
  js: () => import("./workspace.element-P-cofyfC.js"),
  meta: {
    entityType: c.workspace.rootElement
  }
}, ue = [
  {
    type: "workspaceView",
    alias: c.workspace.defaultView.alias,
    name: c.workspace.defaultView.name,
    js: () => import("./default.element-epeWRxuL.js"),
    weight: 300,
    meta: {
      label: "Default",
      pathname: "default",
      icon: "icon-box"
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: j
      }
    ]
  },
  {
    type: "workspaceView",
    alias: c.workspace.settingView.alias,
    name: c.workspace.settingView.name,
    js: () => import("./index-bYspDrjv.js"),
    weight: 200,
    meta: {
      label: "Settings",
      pathname: "settings",
      icon: "icon-box"
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: j
      }
    ]
  }
], le = [], H = [ce, ...ue, ...le], me = [
  {
    type: "globalContext",
    alias: "uSync.GlobalContext.Actions",
    name: "uSync Action Context",
    js: () => import("./action.context-03zE6EM4.js")
  }
], fe = (t, e) => {
  console.log(H), e.registerMany(
    [
      ...me,
      ...ie,
      ...H
    ]
  );
};
export {
  fe as onInit,
  ee as uSyncActionDataSource,
  pe as uSyncActionRepository
};
//# sourceMappingURL=assets.js.map
