import { UmbElementMixin as _t } from "@umbraco-cms/backoffice/element-api";
import { UMB_NOTIFICATION_CONTEXT_TOKEN as ft } from "@umbraco-cms/backoffice/notification";
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const T = window, K = T.ShadowRoot && (T.ShadyCSS === void 0 || T.ShadyCSS.nativeShadow) && "adoptedStyleSheets" in Document.prototype && "replace" in CSSStyleSheet.prototype, ht = Symbol(), Z = /* @__PURE__ */ new WeakMap();
let At = class {
  constructor(t, e, i) {
    if (this._$cssResult$ = !0, i !== ht)
      throw Error("CSSResult is not constructable. Use `unsafeCSS` or `css` instead.");
    this.cssText = t, this.t = e;
  }
  get styleSheet() {
    let t = this.o;
    const e = this.t;
    if (K && t === void 0) {
      const i = e !== void 0 && e.length === 1;
      i && (t = Z.get(e)), t === void 0 && ((this.o = t = new CSSStyleSheet()).replaceSync(this.cssText), i && Z.set(e, t));
    }
    return t;
  }
  toString() {
    return this.cssText;
  }
};
const mt = (n) => new At(typeof n == "string" ? n : n + "", void 0, ht), yt = (n, t) => {
  K ? n.adoptedStyleSheets = t.map((e) => e instanceof CSSStyleSheet ? e : e.styleSheet) : t.forEach((e) => {
    const i = document.createElement("style"), s = T.litNonce;
    s !== void 0 && i.setAttribute("nonce", s), i.textContent = e.cssText, n.appendChild(i);
  });
}, J = K ? (n) => n : (n) => n instanceof CSSStyleSheet ? ((t) => {
  let e = "";
  for (const i of t.cssRules)
    e += i.cssText;
  return mt(e);
})(n) : n;
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
var k;
const O = window, F = O.trustedTypes, gt = F ? F.emptyScript : "", G = O.reactiveElementPolyfillSupport, j = { toAttribute(n, t) {
  switch (t) {
    case Boolean:
      n = n ? gt : null;
      break;
    case Object:
    case Array:
      n = n == null ? n : JSON.stringify(n);
  }
  return n;
}, fromAttribute(n, t) {
  let e = n;
  switch (t) {
    case Boolean:
      e = n !== null;
      break;
    case Number:
      e = n === null ? null : Number(n);
      break;
    case Object:
    case Array:
      try {
        e = JSON.parse(n);
      } catch {
        e = null;
      }
  }
  return e;
} }, at = (n, t) => t !== n && (t == t || n == n), R = { attribute: !0, type: String, converter: j, reflect: !1, hasChanged: at }, V = "finalized";
let m = class extends HTMLElement {
  constructor() {
    super(), this._$Ei = /* @__PURE__ */ new Map(), this.isUpdatePending = !1, this.hasUpdated = !1, this._$El = null, this._$Eu();
  }
  static addInitializer(t) {
    var e;
    this.finalize(), ((e = this.h) !== null && e !== void 0 ? e : this.h = []).push(t);
  }
  static get observedAttributes() {
    this.finalize();
    const t = [];
    return this.elementProperties.forEach((e, i) => {
      const s = this._$Ep(i, e);
      s !== void 0 && (this._$Ev.set(s, i), t.push(s));
    }), t;
  }
  static createProperty(t, e = R) {
    if (e.state && (e.attribute = !1), this.finalize(), this.elementProperties.set(t, e), !e.noAccessor && !this.prototype.hasOwnProperty(t)) {
      const i = typeof t == "symbol" ? Symbol() : "__" + t, s = this.getPropertyDescriptor(t, i, e);
      s !== void 0 && Object.defineProperty(this.prototype, t, s);
    }
  }
  static getPropertyDescriptor(t, e, i) {
    return { get() {
      return this[e];
    }, set(s) {
      const r = this[t];
      this[e] = s, this.requestUpdate(t, r, i);
    }, configurable: !0, enumerable: !0 };
  }
  static getPropertyOptions(t) {
    return this.elementProperties.get(t) || R;
  }
  static finalize() {
    if (this.hasOwnProperty(V))
      return !1;
    this[V] = !0;
    const t = Object.getPrototypeOf(this);
    if (t.finalize(), t.h !== void 0 && (this.h = [...t.h]), this.elementProperties = new Map(t.elementProperties), this._$Ev = /* @__PURE__ */ new Map(), this.hasOwnProperty("properties")) {
      const e = this.properties, i = [...Object.getOwnPropertyNames(e), ...Object.getOwnPropertySymbols(e)];
      for (const s of i)
        this.createProperty(s, e[s]);
    }
    return this.elementStyles = this.finalizeStyles(this.styles), !0;
  }
  static finalizeStyles(t) {
    const e = [];
    if (Array.isArray(t)) {
      const i = new Set(t.flat(1 / 0).reverse());
      for (const s of i)
        e.unshift(J(s));
    } else
      t !== void 0 && e.push(J(t));
    return e;
  }
  static _$Ep(t, e) {
    const i = e.attribute;
    return i === !1 ? void 0 : typeof i == "string" ? i : typeof t == "string" ? t.toLowerCase() : void 0;
  }
  _$Eu() {
    var t;
    this._$E_ = new Promise((e) => this.enableUpdating = e), this._$AL = /* @__PURE__ */ new Map(), this._$Eg(), this.requestUpdate(), (t = this.constructor.h) === null || t === void 0 || t.forEach((e) => e(this));
  }
  addController(t) {
    var e, i;
    ((e = this._$ES) !== null && e !== void 0 ? e : this._$ES = []).push(t), this.renderRoot !== void 0 && this.isConnected && ((i = t.hostConnected) === null || i === void 0 || i.call(t));
  }
  removeController(t) {
    var e;
    (e = this._$ES) === null || e === void 0 || e.splice(this._$ES.indexOf(t) >>> 0, 1);
  }
  _$Eg() {
    this.constructor.elementProperties.forEach((t, e) => {
      this.hasOwnProperty(e) && (this._$Ei.set(e, this[e]), delete this[e]);
    });
  }
  createRenderRoot() {
    var t;
    const e = (t = this.shadowRoot) !== null && t !== void 0 ? t : this.attachShadow(this.constructor.shadowRootOptions);
    return yt(e, this.constructor.elementStyles), e;
  }
  connectedCallback() {
    var t;
    this.renderRoot === void 0 && (this.renderRoot = this.createRenderRoot()), this.enableUpdating(!0), (t = this._$ES) === null || t === void 0 || t.forEach((e) => {
      var i;
      return (i = e.hostConnected) === null || i === void 0 ? void 0 : i.call(e);
    });
  }
  enableUpdating(t) {
  }
  disconnectedCallback() {
    var t;
    (t = this._$ES) === null || t === void 0 || t.forEach((e) => {
      var i;
      return (i = e.hostDisconnected) === null || i === void 0 ? void 0 : i.call(e);
    });
  }
  attributeChangedCallback(t, e, i) {
    this._$AK(t, i);
  }
  _$EO(t, e, i = R) {
    var s;
    const r = this.constructor._$Ep(t, i);
    if (r !== void 0 && i.reflect === !0) {
      const o = (((s = i.converter) === null || s === void 0 ? void 0 : s.toAttribute) !== void 0 ? i.converter : j).toAttribute(e, i.type);
      this._$El = t, o == null ? this.removeAttribute(r) : this.setAttribute(r, o), this._$El = null;
    }
  }
  _$AK(t, e) {
    var i;
    const s = this.constructor, r = s._$Ev.get(t);
    if (r !== void 0 && this._$El !== r) {
      const o = s.getPropertyOptions(r), a = typeof o.converter == "function" ? { fromAttribute: o.converter } : ((i = o.converter) === null || i === void 0 ? void 0 : i.fromAttribute) !== void 0 ? o.converter : j;
      this._$El = r, this[r] = a.fromAttribute(e, o.type), this._$El = null;
    }
  }
  requestUpdate(t, e, i) {
    let s = !0;
    t !== void 0 && (((i = i || this.constructor.getPropertyOptions(t)).hasChanged || at)(this[t], e) ? (this._$AL.has(t) || this._$AL.set(t, e), i.reflect === !0 && this._$El !== t && (this._$EC === void 0 && (this._$EC = /* @__PURE__ */ new Map()), this._$EC.set(t, i))) : s = !1), !this.isUpdatePending && s && (this._$E_ = this._$Ej());
  }
  async _$Ej() {
    this.isUpdatePending = !0;
    try {
      await this._$E_;
    } catch (e) {
      Promise.reject(e);
    }
    const t = this.scheduleUpdate();
    return t != null && await t, !this.isUpdatePending;
  }
  scheduleUpdate() {
    return this.performUpdate();
  }
  performUpdate() {
    var t;
    if (!this.isUpdatePending)
      return;
    this.hasUpdated, this._$Ei && (this._$Ei.forEach((s, r) => this[r] = s), this._$Ei = void 0);
    let e = !1;
    const i = this._$AL;
    try {
      e = this.shouldUpdate(i), e ? (this.willUpdate(i), (t = this._$ES) === null || t === void 0 || t.forEach((s) => {
        var r;
        return (r = s.hostUpdate) === null || r === void 0 ? void 0 : r.call(s);
      }), this.update(i)) : this._$Ek();
    } catch (s) {
      throw e = !1, this._$Ek(), s;
    }
    e && this._$AE(i);
  }
  willUpdate(t) {
  }
  _$AE(t) {
    var e;
    (e = this._$ES) === null || e === void 0 || e.forEach((i) => {
      var s;
      return (s = i.hostUpdated) === null || s === void 0 ? void 0 : s.call(i);
    }), this.hasUpdated || (this.hasUpdated = !0, this.firstUpdated(t)), this.updated(t);
  }
  _$Ek() {
    this._$AL = /* @__PURE__ */ new Map(), this.isUpdatePending = !1;
  }
  get updateComplete() {
    return this.getUpdateComplete();
  }
  getUpdateComplete() {
    return this._$E_;
  }
  shouldUpdate(t) {
    return !0;
  }
  update(t) {
    this._$EC !== void 0 && (this._$EC.forEach((e, i) => this._$EO(i, this[i], e)), this._$EC = void 0), this._$Ek();
  }
  updated(t) {
  }
  firstUpdated(t) {
  }
};
m[V] = !0, m.elementProperties = /* @__PURE__ */ new Map(), m.elementStyles = [], m.shadowRootOptions = { mode: "open" }, G == null || G({ ReactiveElement: m }), ((k = O.reactiveElementVersions) !== null && k !== void 0 ? k : O.reactiveElementVersions = []).push("1.6.3");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
var L;
const H = window, y = H.trustedTypes, X = y ? y.createPolicy("lit-html", { createHTML: (n) => n }) : void 0, W = "$lit$", v = `lit$${(Math.random() + "").slice(9)}$`, dt = "?" + v, Et = `<${dt}>`, A = document, b = () => A.createComment(""), w = (n) => n === null || typeof n != "object" && typeof n != "function", ct = Array.isArray, St = (n) => ct(n) || typeof (n == null ? void 0 : n[Symbol.iterator]) == "function", D = `[ 	
\f\r]`, S = /<(?:(!--|\/[^a-zA-Z])|(\/?[a-zA-Z][^>\s]*)|(\/?$))/g, Q = /-->/g, Y = />/g, _ = RegExp(`>|${D}(?:([^\\s"'>=/]+)(${D}*=${D}*(?:[^ 	
\f\r"'\`<>=]|("|')|))|$)`, "g"), tt = /'/g, et = /"/g, ut = /^(?:script|style|textarea|title)$/i, Ct = (n) => (t, ...e) => ({ _$litType$: n, strings: t, values: e }), bt = Ct(1), g = Symbol.for("lit-noChange"), u = Symbol.for("lit-nothing"), it = /* @__PURE__ */ new WeakMap(), f = A.createTreeWalker(A, 129, null, !1);
function pt(n, t) {
  if (!Array.isArray(n) || !n.hasOwnProperty("raw"))
    throw Error("invalid template strings array");
  return X !== void 0 ? X.createHTML(t) : t;
}
const wt = (n, t) => {
  const e = n.length - 1, i = [];
  let s, r = t === 2 ? "<svg>" : "", o = S;
  for (let a = 0; a < e; a++) {
    const l = n[a];
    let h, d, c = -1, p = 0;
    for (; p < l.length && (o.lastIndex = p, d = o.exec(l), d !== null); )
      p = o.lastIndex, o === S ? d[1] === "!--" ? o = Q : d[1] !== void 0 ? o = Y : d[2] !== void 0 ? (ut.test(d[2]) && (s = RegExp("</" + d[2], "g")), o = _) : d[3] !== void 0 && (o = _) : o === _ ? d[0] === ">" ? (o = s ?? S, c = -1) : d[1] === void 0 ? c = -2 : (c = o.lastIndex - d[2].length, h = d[1], o = d[3] === void 0 ? _ : d[3] === '"' ? et : tt) : o === et || o === tt ? o = _ : o === Q || o === Y ? o = S : (o = _, s = void 0);
    const $ = o === _ && n[a + 1].startsWith("/>") ? " " : "";
    r += o === S ? l + Et : c >= 0 ? (i.push(h), l.slice(0, c) + W + l.slice(c) + v + $) : l + v + (c === -2 ? (i.push(void 0), a) : $);
  }
  return [pt(n, r + (n[e] || "<?>") + (t === 2 ? "</svg>" : "")), i];
};
class x {
  constructor({ strings: t, _$litType$: e }, i) {
    let s;
    this.parts = [];
    let r = 0, o = 0;
    const a = t.length - 1, l = this.parts, [h, d] = wt(t, e);
    if (this.el = x.createElement(h, i), f.currentNode = this.el.content, e === 2) {
      const c = this.el.content, p = c.firstChild;
      p.remove(), c.append(...p.childNodes);
    }
    for (; (s = f.nextNode()) !== null && l.length < a; ) {
      if (s.nodeType === 1) {
        if (s.hasAttributes()) {
          const c = [];
          for (const p of s.getAttributeNames())
            if (p.endsWith(W) || p.startsWith(v)) {
              const $ = d[o++];
              if (c.push(p), $ !== void 0) {
                const vt = s.getAttribute($.toLowerCase() + W).split(v), U = /([.?@])?(.*)/.exec($);
                l.push({ type: 1, index: r, name: U[2], strings: vt, ctor: U[1] === "." ? Pt : U[1] === "?" ? Tt : U[1] === "@" ? Nt : M });
              } else
                l.push({ type: 6, index: r });
            }
          for (const p of c)
            s.removeAttribute(p);
        }
        if (ut.test(s.tagName)) {
          const c = s.textContent.split(v), p = c.length - 1;
          if (p > 0) {
            s.textContent = y ? y.emptyScript : "";
            for (let $ = 0; $ < p; $++)
              s.append(c[$], b()), f.nextNode(), l.push({ type: 2, index: ++r });
            s.append(c[p], b());
          }
        }
      } else if (s.nodeType === 8)
        if (s.data === dt)
          l.push({ type: 2, index: r });
        else {
          let c = -1;
          for (; (c = s.data.indexOf(v, c + 1)) !== -1; )
            l.push({ type: 7, index: r }), c += v.length - 1;
        }
      r++;
    }
  }
  static createElement(t, e) {
    const i = A.createElement("template");
    return i.innerHTML = t, i;
  }
}
function E(n, t, e = n, i) {
  var s, r, o, a;
  if (t === g)
    return t;
  let l = i !== void 0 ? (s = e._$Co) === null || s === void 0 ? void 0 : s[i] : e._$Cl;
  const h = w(t) ? void 0 : t._$litDirective$;
  return (l == null ? void 0 : l.constructor) !== h && ((r = l == null ? void 0 : l._$AO) === null || r === void 0 || r.call(l, !1), h === void 0 ? l = void 0 : (l = new h(n), l._$AT(n, e, i)), i !== void 0 ? ((o = (a = e)._$Co) !== null && o !== void 0 ? o : a._$Co = [])[i] = l : e._$Cl = l), l !== void 0 && (t = E(n, l._$AS(n, t.values), l, i)), t;
}
class xt {
  constructor(t, e) {
    this._$AV = [], this._$AN = void 0, this._$AD = t, this._$AM = e;
  }
  get parentNode() {
    return this._$AM.parentNode;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  u(t) {
    var e;
    const { el: { content: i }, parts: s } = this._$AD, r = ((e = t == null ? void 0 : t.creationScope) !== null && e !== void 0 ? e : A).importNode(i, !0);
    f.currentNode = r;
    let o = f.nextNode(), a = 0, l = 0, h = s[0];
    for (; h !== void 0; ) {
      if (a === h.index) {
        let d;
        h.type === 2 ? d = new P(o, o.nextSibling, this, t) : h.type === 1 ? d = new h.ctor(o, h.name, h.strings, this, t) : h.type === 6 && (d = new Ot(o, this, t)), this._$AV.push(d), h = s[++l];
      }
      a !== (h == null ? void 0 : h.index) && (o = f.nextNode(), a++);
    }
    return f.currentNode = A, r;
  }
  v(t) {
    let e = 0;
    for (const i of this._$AV)
      i !== void 0 && (i.strings !== void 0 ? (i._$AI(t, i, e), e += i.strings.length - 2) : i._$AI(t[e])), e++;
  }
}
class P {
  constructor(t, e, i, s) {
    var r;
    this.type = 2, this._$AH = u, this._$AN = void 0, this._$AA = t, this._$AB = e, this._$AM = i, this.options = s, this._$Cp = (r = s == null ? void 0 : s.isConnected) === null || r === void 0 || r;
  }
  get _$AU() {
    var t, e;
    return (e = (t = this._$AM) === null || t === void 0 ? void 0 : t._$AU) !== null && e !== void 0 ? e : this._$Cp;
  }
  get parentNode() {
    let t = this._$AA.parentNode;
    const e = this._$AM;
    return e !== void 0 && (t == null ? void 0 : t.nodeType) === 11 && (t = e.parentNode), t;
  }
  get startNode() {
    return this._$AA;
  }
  get endNode() {
    return this._$AB;
  }
  _$AI(t, e = this) {
    t = E(this, t, e), w(t) ? t === u || t == null || t === "" ? (this._$AH !== u && this._$AR(), this._$AH = u) : t !== this._$AH && t !== g && this._(t) : t._$litType$ !== void 0 ? this.g(t) : t.nodeType !== void 0 ? this.$(t) : St(t) ? this.T(t) : this._(t);
  }
  k(t) {
    return this._$AA.parentNode.insertBefore(t, this._$AB);
  }
  $(t) {
    this._$AH !== t && (this._$AR(), this._$AH = this.k(t));
  }
  _(t) {
    this._$AH !== u && w(this._$AH) ? this._$AA.nextSibling.data = t : this.$(A.createTextNode(t)), this._$AH = t;
  }
  g(t) {
    var e;
    const { values: i, _$litType$: s } = t, r = typeof s == "number" ? this._$AC(t) : (s.el === void 0 && (s.el = x.createElement(pt(s.h, s.h[0]), this.options)), s);
    if (((e = this._$AH) === null || e === void 0 ? void 0 : e._$AD) === r)
      this._$AH.v(i);
    else {
      const o = new xt(r, this), a = o.u(this.options);
      o.v(i), this.$(a), this._$AH = o;
    }
  }
  _$AC(t) {
    let e = it.get(t.strings);
    return e === void 0 && it.set(t.strings, e = new x(t)), e;
  }
  T(t) {
    ct(this._$AH) || (this._$AH = [], this._$AR());
    const e = this._$AH;
    let i, s = 0;
    for (const r of t)
      s === e.length ? e.push(i = new P(this.k(b()), this.k(b()), this, this.options)) : i = e[s], i._$AI(r), s++;
    s < e.length && (this._$AR(i && i._$AB.nextSibling, s), e.length = s);
  }
  _$AR(t = this._$AA.nextSibling, e) {
    var i;
    for ((i = this._$AP) === null || i === void 0 || i.call(this, !1, !0, e); t && t !== this._$AB; ) {
      const s = t.nextSibling;
      t.remove(), t = s;
    }
  }
  setConnected(t) {
    var e;
    this._$AM === void 0 && (this._$Cp = t, (e = this._$AP) === null || e === void 0 || e.call(this, t));
  }
}
class M {
  constructor(t, e, i, s, r) {
    this.type = 1, this._$AH = u, this._$AN = void 0, this.element = t, this.name = e, this._$AM = s, this.options = r, i.length > 2 || i[0] !== "" || i[1] !== "" ? (this._$AH = Array(i.length - 1).fill(new String()), this.strings = i) : this._$AH = u;
  }
  get tagName() {
    return this.element.tagName;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(t, e = this, i, s) {
    const r = this.strings;
    let o = !1;
    if (r === void 0)
      t = E(this, t, e, 0), o = !w(t) || t !== this._$AH && t !== g, o && (this._$AH = t);
    else {
      const a = t;
      let l, h;
      for (t = r[0], l = 0; l < r.length - 1; l++)
        h = E(this, a[i + l], e, l), h === g && (h = this._$AH[l]), o || (o = !w(h) || h !== this._$AH[l]), h === u ? t = u : t !== u && (t += (h ?? "") + r[l + 1]), this._$AH[l] = h;
    }
    o && !s && this.j(t);
  }
  j(t) {
    t === u ? this.element.removeAttribute(this.name) : this.element.setAttribute(this.name, t ?? "");
  }
}
class Pt extends M {
  constructor() {
    super(...arguments), this.type = 3;
  }
  j(t) {
    this.element[this.name] = t === u ? void 0 : t;
  }
}
const Ut = y ? y.emptyScript : "";
class Tt extends M {
  constructor() {
    super(...arguments), this.type = 4;
  }
  j(t) {
    t && t !== u ? this.element.setAttribute(this.name, Ut) : this.element.removeAttribute(this.name);
  }
}
class Nt extends M {
  constructor(t, e, i, s, r) {
    super(t, e, i, s, r), this.type = 5;
  }
  _$AI(t, e = this) {
    var i;
    if ((t = (i = E(this, t, e, 0)) !== null && i !== void 0 ? i : u) === g)
      return;
    const s = this._$AH, r = t === u && s !== u || t.capture !== s.capture || t.once !== s.once || t.passive !== s.passive, o = t !== u && (s === u || r);
    r && this.element.removeEventListener(this.name, this, s), o && this.element.addEventListener(this.name, this, t), this._$AH = t;
  }
  handleEvent(t) {
    var e, i;
    typeof this._$AH == "function" ? this._$AH.call((i = (e = this.options) === null || e === void 0 ? void 0 : e.host) !== null && i !== void 0 ? i : this.element, t) : this._$AH.handleEvent(t);
  }
}
class Ot {
  constructor(t, e, i) {
    this.element = t, this.type = 6, this._$AN = void 0, this._$AM = e, this.options = i;
  }
  get _$AU() {
    return this._$AM._$AU;
  }
  _$AI(t) {
    E(this, t);
  }
}
const st = H.litHtmlPolyfillSupport;
st == null || st(x, P), ((L = H.litHtmlVersions) !== null && L !== void 0 ? L : H.litHtmlVersions = []).push("2.8.0");
const Ht = (n, t, e) => {
  var i, s;
  const r = (i = e == null ? void 0 : e.renderBefore) !== null && i !== void 0 ? i : t;
  let o = r._$litPart$;
  if (o === void 0) {
    const a = (s = e == null ? void 0 : e.renderBefore) !== null && s !== void 0 ? s : null;
    r._$litPart$ = o = new P(t.insertBefore(b(), a), a, void 0, e ?? {});
  }
  return o._$AI(n), o;
};
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
var I, B;
class C extends m {
  constructor() {
    super(...arguments), this.renderOptions = { host: this }, this._$Do = void 0;
  }
  createRenderRoot() {
    var t, e;
    const i = super.createRenderRoot();
    return (t = (e = this.renderOptions).renderBefore) !== null && t !== void 0 || (e.renderBefore = i.firstChild), i;
  }
  update(t) {
    const e = this.render();
    this.hasUpdated || (this.renderOptions.isConnected = this.isConnected), super.update(t), this._$Do = Ht(e, this.renderRoot, this.renderOptions);
  }
  connectedCallback() {
    var t;
    super.connectedCallback(), (t = this._$Do) === null || t === void 0 || t.setConnected(!0);
  }
  disconnectedCallback() {
    var t;
    super.disconnectedCallback(), (t = this._$Do) === null || t === void 0 || t.setConnected(!1);
  }
  render() {
    return g;
  }
}
C.finalized = !0, C._$litElement$ = !0, (I = globalThis.litElementHydrateSupport) === null || I === void 0 || I.call(globalThis, { LitElement: C });
const nt = globalThis.litElementPolyfillSupport;
nt == null || nt({ LitElement: C });
((B = globalThis.litElementVersions) !== null && B !== void 0 ? B : globalThis.litElementVersions = []).push("3.3.3");
/**
 * @license
 * Copyright 2017 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
const Mt = (n) => (t) => typeof t == "function" ? ((e, i) => (customElements.define(e, i), i))(n, t) : ((e, i) => {
  const { kind: s, elements: r } = i;
  return { kind: s, elements: r, finisher(o) {
    customElements.define(e, o);
  } };
})(n, t);
/**
 * @license
 * Copyright 2021 Google LLC
 * SPDX-License-Identifier: BSD-3-Clause
 */
var z;
((z = window.HTMLSlotElement) === null || z === void 0 ? void 0 : z.prototype.assignedElements) != null;
var kt = Object.defineProperty, Rt = Object.getOwnPropertyDescriptor, Lt = (n, t, e, i) => {
  for (var s = i > 1 ? void 0 : i ? Rt(t, e) : t, r = n.length - 1, o; r >= 0; r--)
    (o = n[r]) && (s = (i ? o(t, e, s) : o(s)) || s);
  return i && s && kt(t, e, s), s;
}, $t = (n, t, e) => {
  if (!t.has(n))
    throw TypeError("Cannot " + e);
}, ot = (n, t, e) => ($t(n, t, "read from private field"), e ? e.call(n) : t.get(n)), rt = (n, t, e) => {
  if (t.has(n))
    throw TypeError("Cannot add the same private member more than once");
  t instanceof WeakSet ? t.add(n) : t.set(n, e);
}, Dt = (n, t, e, i) => ($t(n, t, "write to private field"), i ? i.call(n, e) : t.set(n, e), e), N, q;
let lt = class extends _t(C) {
  constructor() {
    super(), rt(this, N, void 0), rt(this, q, () => {
      var n;
      (n = ot(this, N)) == null || n.peek("positive", { data: { message: "#h5yr" } });
    }), this.consumeContext(ft, (n) => {
      Dt(this, N, n);
    });
  }
  render() {
    return bt`
			<uui-box headline="Welcome">
				<p>A TypeScript Lit Dashboard</p>
				<uui-button look="primary" label="Click me" @click=${ot(this, q)}></uui-button>
			</uui-box>
		`;
  }
};
N = /* @__PURE__ */ new WeakMap();
q = /* @__PURE__ */ new WeakMap();
lt = Lt([
  Mt("my-element")
], lt);
export {
  lt as default
};
//# sourceMappingURL=usync.js.map
