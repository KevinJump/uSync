import { html as r, css as v, state as _, customElement as H } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as g } from "@umbraco-cms/backoffice/lit-element";
import { c as y } from "./index-BGXA9jiq.js";
import { umbConfirmModal as w } from "@umbraco-cms/backoffice/modal";
class m {
  static clearHistory(e) {
    return ((e == null ? void 0 : e.client) ?? y).get({
      url: "/umbraco/usync/api/v1/history/ClearHistory",
      ...e
    });
  }
  static getHistory(e) {
    return ((e == null ? void 0 : e.client) ?? y).get({
      url: "/umbraco/usync/api/v1/history/GetHistory",
      ...e
    });
  }
  static loadHistory(e) {
    return ((e == null ? void 0 : e.client) ?? y).get({
      url: "/umbraco/usync/api/v1/history/HistoryInfo",
      ...e
    });
  }
}
const z = {
  dateStyle: "long",
  timeStyle: "short"
};
var S = Object.defineProperty, $ = Object.getOwnPropertyDescriptor, b = (t) => {
  throw TypeError(t);
}, d = (t, e, a, c) => {
  for (var l = c > 1 ? void 0 : c ? $(e, a) : e, n = t.length - 1, s; n >= 0; n--)
    (s = t[n]) && (l = (c ? s(e, a, l) : s(l)) || l);
  return c && l && S(e, a, l), l;
}, C = (t, e, a) => e.has(t) || b("Cannot " + a), E = (t, e, a) => e.has(t) ? b("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, a), o = (t, e, a) => (C(t, e, "access private method"), a), i, h, f, p;
let u = class extends g {
  constructor() {
    super(), E(this, i), this.history = [];
  }
  async connectedCallback() {
    super.connectedCallback(), await o(this, i, h).call(this);
  }
  render() {
    var t;
    return ((t = this.history) == null ? void 0 : t.length) > 0 ? r`${this.renderHistory()}` : r`${this.renderEmpty()}`;
  }
  renderEmpty() {
    return r`<umb-empty-state
      ><h2>
        <uui-icon name="usync-logo"></uui-icon
        ><umb-localize key="uSyncHistory_empty"></umb-localize></h2
    ></umb-empty-state>`;
  }
  renderHistory() {
    const t = this.history.map((e) => r`<uui-table-row
        ><uui-table-cell>${this.renderIcon(e)}</uui-table-cell
        ><uui-table-cell
          ><umb-localize
            key="uSync_${e.method}"
          ></umb-localize></uui-table-cell
        ><uui-table-cell>${e.changes}/${e.total}</uui-table-cell
        ><uui-table-cell>${e.username}</uui-table-cell>
        <uui-table-cell
          >${e.date, this.localize.date(e.date, z)}</uui-table-cell
        ></uui-table-row
      >`);
    return r`<umb-body-layout
      ><uui-table>${this.renderTableHead()}${t}</uui-table>
      <div slot="footer-info" class="footer">
        <uui-button
          label="Clear"
          look="primary"
          color="danger"
          @click=${o(this, i, f)}
        ></uui-button></div
    ></umb-body-layout>`;
  }
  renderTableHead() {
    return r`<uui-table-head
      ><uui-table-head-cell
        ><umb-icon
          name="usync-logo"
          style="font-size: 20px; color: #aaa"
        ></umb-icon></uui-table-head-cell
      ><uui-table-head-cell>Method</uui-table-head-cell
      ><uui-table-head-cell>Changes</uui-table-head-cell
      ><uui-table-head-cell>User</uui-table-head-cell
      ><uui-table-head-cell>Date</uui-table-head-cell></uui-table-head
    >`;
  }
  renderIcon(t) {
    switch (t.method) {
      case "Import":
        return r`<umb-icon name="icon-box"></umb-icon>`;
      case "Export":
        return r`<umb-icon name="icon-shipping"></umb-icon>`;
      default:
        return r`<umb-icon name="icon-document-spreadsheet"></umb-icon>`;
    }
  }
};
i = /* @__PURE__ */ new WeakSet();
h = async function() {
  this.history = (await m.getHistory()).data ?? [];
};
f = function(t) {
  w(this, {
    headline: this.localize.term("uSyncHistory_clear"),
    content: this.localize.term("uSyncHistory_clearWarning"),
    color: "danger",
    confirmLabel: this.localize.term("uSyncHistory_clear")
  }).then(async () => {
    await o(this, i, p).call(this), await o(this, i, h).call(this);
  }).catch(() => {
  });
};
p = async function() {
  await m.clearHistory();
};
u.styles = v`
    .footer {
      width: 100%;
      display: flex;
      justify-content: flex-end;
    }

    umb-empty-state {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      left: 0;
      right: 0;
      margin: 0 auto;
      text-align: center;
      color: var(--uui-color-border);
      z-index: 0;
    }

    umb-empty-state h2 {
      font-size: var(--uui-type-h2-size);
    }

    umb-empty-state uui-icon {
      position: relative;
      top: var(--uui-size-2);
    }
  `;
d([
  _()
], u.prototype, "history", 2);
u = d([
  H("usync-history-view")
], u);
const P = u;
export {
  P as default,
  u as uSyncHistoryElement
};
//# sourceMappingURL=history-view.element-BU5CDzls.js.map
