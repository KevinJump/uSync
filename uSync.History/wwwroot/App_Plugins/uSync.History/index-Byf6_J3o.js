var T = Object.defineProperty;
var A = (e, a, t) => a in e ? T(e, a, { enumerable: !0, configurable: !0, writable: !0, value: t }) : e[a] = t;
var g = (e, a, t) => A(e, typeof a != "symbol" ? a + "" : a, t);
import { uSyncConstants as R } from "@jumoo/uSync";
import { UMB_AUTH_CONTEXT as _ } from "@umbraco-cms/backoffice/auth";
const I = {
  type: "localization",
  alias: "usync.history.localisation",
  name: "uSync History",
  js: () => import("./en-2aBDgkCk.js"),
  meta: {
    culture: "en"
  }
}, z = [I], k = {
  type: "workspaceView",
  alias: "historyView",
  name: "uSync history",
  js: () => import("./history-view.element-Dmaa43_J.js"),
  weight: 125,
  meta: {
    label: "History",
    pathname: "history",
    icon: "icon-calendar-alt"
  },
  conditions: [
    {
      alias: "Umb.Condition.WorkspaceAlias",
      match: R.workspace.alias
    }
  ]
}, E = [k], W = {
  type: "modal",
  alias: "usync.history.modal",
  name: "uSync History",
  js: () => import("./history-modal.element-CoNqy51l.js")
}, H = [W];
var D = async (e, a) => {
  let t = typeof a == "function" ? await a(e) : a;
  if (t) return e.scheme === "bearer" ? `Bearer ${t}` : e.scheme === "basic" ? `Basic ${btoa(t)}` : t;
}, N = { bodySerializer: (e) => JSON.stringify(e, (a, t) => typeof t == "bigint" ? t.toString() : t) }, P = (e) => {
  switch (e) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, B = (e) => {
  switch (e) {
    case "form":
      return ",";
    case "pipeDelimited":
      return "|";
    case "spaceDelimited":
      return "%20";
    default:
      return ",";
  }
}, J = (e) => {
  switch (e) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, S = ({ allowReserved: e, explode: a, name: t, style: n, value: o }) => {
  if (!a) {
    let r = (e ? o : o.map((l) => encodeURIComponent(l))).join(B(n));
    switch (n) {
      case "label":
        return `.${r}`;
      case "matrix":
        return `;${t}=${r}`;
      case "simple":
        return r;
      default:
        return `${t}=${r}`;
    }
  }
  let i = P(n), s = o.map((r) => n === "label" || n === "simple" ? e ? r : encodeURIComponent(r) : b({ allowReserved: e, name: t, value: r })).join(i);
  return n === "label" || n === "matrix" ? i + s : s;
}, b = ({ allowReserved: e, name: a, value: t }) => {
  if (t == null) return "";
  if (typeof t == "object") throw new Error("Deeply-nested arrays/objects aren’t supported. Provide your own `querySerializer()` to handle these.");
  return `${a}=${e ? t : encodeURIComponent(t)}`;
}, $ = ({ allowReserved: e, explode: a, name: t, style: n, value: o }) => {
  if (o instanceof Date) return `${t}=${o.toISOString()}`;
  if (n !== "deepObject" && !a) {
    let r = [];
    Object.entries(o).forEach(([d, f]) => {
      r = [...r, d, e ? f : encodeURIComponent(f)];
    });
    let l = r.join(",");
    switch (n) {
      case "form":
        return `${t}=${l}`;
      case "label":
        return `.${l}`;
      case "matrix":
        return `;${t}=${l}`;
      default:
        return l;
    }
  }
  let i = J(n), s = Object.entries(o).map(([r, l]) => b({ allowReserved: e, name: n === "deepObject" ? `${t}[${r}]` : r, value: l })).join(i);
  return n === "label" || n === "matrix" ? i + s : s;
}, V = /\{[^{}]+\}/g, L = ({ path: e, url: a }) => {
  let t = a, n = a.match(V);
  if (n) for (let o of n) {
    let i = !1, s = o.substring(1, o.length - 1), r = "simple";
    s.endsWith("*") && (i = !0, s = s.substring(0, s.length - 1)), s.startsWith(".") ? (s = s.substring(1), r = "label") : s.startsWith(";") && (s = s.substring(1), r = "matrix");
    let l = e[s];
    if (l == null) continue;
    if (Array.isArray(l)) {
      t = t.replace(o, S({ explode: i, name: s, style: r, value: l }));
      continue;
    }
    if (typeof l == "object") {
      t = t.replace(o, $({ explode: i, name: s, style: r, value: l }));
      continue;
    }
    if (r === "matrix") {
      t = t.replace(o, `;${b({ name: s, value: l })}`);
      continue;
    }
    let d = encodeURIComponent(r === "label" ? `.${l}` : l);
    t = t.replace(o, d);
  }
  return t;
}, C = ({ allowReserved: e, array: a, object: t } = {}) => (n) => {
  let o = [];
  if (n && typeof n == "object") for (let i in n) {
    let s = n[i];
    if (s != null) if (Array.isArray(s)) {
      let r = S({ allowReserved: e, explode: !0, name: i, style: "form", value: s, ...a });
      r && o.push(r);
    } else if (typeof s == "object") {
      let r = $({ allowReserved: e, explode: !0, name: i, style: "deepObject", value: s, ...t });
      r && o.push(r);
    } else {
      let r = b({ allowReserved: e, name: i, value: s });
      r && o.push(r);
    }
  }
  return o.join("&");
}, M = (e) => {
  var t;
  if (!e) return "stream";
  let a = (t = e.split(";")[0]) == null ? void 0 : t.trim();
  if (a) {
    if (a.startsWith("application/json") || a.endsWith("+json")) return "json";
    if (a === "multipart/form-data") return "formData";
    if (["application/", "audio/", "image/", "video/"].some((n) => a.startsWith(n))) return "blob";
    if (a.startsWith("text/")) return "text";
  }
}, G = async ({ security: e, ...a }) => {
  for (let t of e) {
    let n = await D(t, a.auth);
    if (!n) continue;
    let o = t.name ?? "Authorization";
    switch (t.in) {
      case "query":
        a.query || (a.query = {}), a.query[o] = n;
        break;
      case "cookie":
        a.headers.append("Cookie", `${o}=${n}`);
        break;
      case "header":
      default:
        a.headers.set(o, n);
        break;
    }
    return;
  }
}, j = (e) => Q({ baseUrl: e.baseUrl, path: e.path, query: e.query, querySerializer: typeof e.querySerializer == "function" ? e.querySerializer : C(e.querySerializer), url: e.url }), Q = ({ baseUrl: e, path: a, query: t, querySerializer: n, url: o }) => {
  let i = o.startsWith("/") ? o : `/${o}`, s = (e ?? "") + i;
  a && (s = L({ path: a, url: s }));
  let r = t ? n(t) : "";
  return r.startsWith("?") && (r = r.substring(1)), r && (s += `?${r}`), s;
}, v = (e, a) => {
  var n;
  let t = { ...e, ...a };
  return (n = t.baseUrl) != null && n.endsWith("/") && (t.baseUrl = t.baseUrl.substring(0, t.baseUrl.length - 1)), t.headers = q(e.headers, a.headers), t;
}, q = (...e) => {
  let a = new Headers();
  for (let t of e) {
    if (!t || typeof t != "object") continue;
    let n = t instanceof Headers ? t.entries() : Object.entries(t);
    for (let [o, i] of n) if (i === null) a.delete(o);
    else if (Array.isArray(i)) for (let s of i) a.append(o, s);
    else i !== void 0 && a.set(o, typeof i == "object" ? JSON.stringify(i) : i);
  }
  return a;
}, w = class {
  constructor() {
    g(this, "_fns");
    this._fns = [];
  }
  clear() {
    this._fns = [];
  }
  getInterceptorIndex(e) {
    return typeof e == "number" ? this._fns[e] ? e : -1 : this._fns.indexOf(e);
  }
  exists(e) {
    let a = this.getInterceptorIndex(e);
    return !!this._fns[a];
  }
  eject(e) {
    let a = this.getInterceptorIndex(e);
    this._fns[a] && (this._fns[a] = null);
  }
  update(e, a) {
    let t = this.getInterceptorIndex(e);
    return this._fns[t] ? (this._fns[t] = a, e) : !1;
  }
  use(e) {
    return this._fns = [...this._fns, e], this._fns.length - 1;
  }
}, X = () => ({ error: new w(), request: new w(), response: new w() }), F = C({ allowReserved: !1, array: { explode: !0, style: "form" }, object: { explode: !0, style: "deepObject" } }), K = { "Content-Type": "application/json" }, O = (e = {}) => ({ ...N, headers: K, parseAs: "auto", querySerializer: F, ...e }), Y = (e = {}) => {
  let a = v(O(), e), t = () => ({ ...a }), n = (s) => (a = v(a, s), t()), o = X(), i = async (s) => {
    let r = { ...a, ...s, fetch: s.fetch ?? a.fetch ?? globalThis.fetch, headers: q(a.headers, s.headers) };
    r.security && await G({ ...r, security: r.security }), r.body && r.bodySerializer && (r.body = r.bodySerializer(r.body)), (r.body === void 0 || r.body === "") && r.headers.delete("Content-Type");
    let l = j(r), d = { redirect: "follow", ...r }, f = new Request(l, d);
    for (let c of o.request._fns) c && (f = await c(f, r));
    let U = r.fetch, u = await U(f);
    for (let c of o.response._fns) c && (u = await c(u, f, r));
    let y = { request: f, response: u };
    if (u.ok) {
      if (u.status === 204 || u.headers.get("Content-Length") === "0") return r.responseStyle === "data" ? {} : { data: {}, ...y };
      let c = (r.parseAs === "auto" ? M(u.headers.get("Content-Type")) : r.parseAs) ?? "json";
      if (c === "stream") return r.responseStyle === "data" ? u.body : { data: u.body, ...y };
      let h = await u[c]();
      return c === "json" && (r.responseValidator && await r.responseValidator(h), r.responseTransformer && (h = await r.responseTransformer(h))), r.responseStyle === "data" ? h : { data: h, ...y };
    }
    let m = await u.text();
    try {
      m = JSON.parse(m);
    } catch {
    }
    let p = m;
    for (let c of o.error._fns) c && (p = await c(m, u, f, r));
    if (p = p || {}, r.throwOnError) throw p;
    return r.responseStyle === "data" ? void 0 : { error: p, ...y };
  };
  return { buildUrl: j, connect: (s) => i({ ...s, method: "CONNECT" }), delete: (s) => i({ ...s, method: "DELETE" }), get: (s) => i({ ...s, method: "GET" }), getConfig: t, head: (s) => i({ ...s, method: "HEAD" }), interceptors: o, options: (s) => i({ ...s, method: "OPTIONS" }), patch: (s) => i({ ...s, method: "PATCH" }), post: (s) => i({ ...s, method: "POST" }), put: (s) => i({ ...s, method: "PUT" }), request: i, setConfig: n, trace: (s) => i({ ...s, method: "TRACE" }) };
};
const x = Y(O({
  baseUrl: "https://localhost:44338"
})), re = (e, a) => {
  console.log("hit"), a.registerMany([...z, ...E, ...H]), e.consumeContext(_, (t) => {
    if (t) {
      var n = t.getOpenApiConfiguration();
      x.setConfig({
        auth: n.token,
        baseUrl: n.base,
        credentials: n.credentials
      }), x.interceptors.request.use(async (o, i) => {
        const s = await t.getLatestToken();
        return o.headers.set("Authorization", `Bearer ${s}`), o;
      });
    }
  });
};
export {
  x as c,
  re as o
};
//# sourceMappingURL=index-Byf6_J3o.js.map
