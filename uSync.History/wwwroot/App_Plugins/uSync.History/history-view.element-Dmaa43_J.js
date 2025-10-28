import { html as l, css as v, state as H, customElement as g } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as w } from "@umbraco-cms/backoffice/lit-element";
import { c as h } from "./index-Byf6_J3o.js";
import { T as z } from "./consts-BD6tjpOQ.js";
import { UmbModalToken as $, umbConfirmModal as E, umbOpenModal as S } from "@umbraco-cms/backoffice/modal";
class m {
  static clearHistory(e) {
    return ((e == null ? void 0 : e.client) ?? h).get({
      url: "/umbraco/usync/api/v1/history/ClearHistory",
      ...e
    });
  }
  static getHistory(e) {
    return ((e == null ? void 0 : e.client) ?? h).get({
      url: "/umbraco/usync/api/v1/history/GetHistory",
      ...e
    });
  }
  static loadHistory(e) {
    return ((e == null ? void 0 : e.client) ?? h).get({
      url: "/umbraco/usync/api/v1/history/HistoryInfo",
      ...e
    });
  }
}
const C = new $("usync.history.modal", {
  modal: {
    type: "sidebar",
    size: "small"
  }
});
var x = Object.defineProperty, O = Object.getOwnPropertyDescriptor, b = (t) => {
  throw TypeError(t);
}, d = (t, e, a, o) => {
  for (var r = o > 1 ? void 0 : o ? O(e, a) : e, n = t.length - 1, s; n >= 0; n--)
    (s = t[n]) && (r = (o ? s(e, a, r) : s(r)) || r);
  return o && r && x(e, a, r), r;
}, T = (t, e, a) => e.has(t) || b("Cannot " + a), k = (t, e, a) => e.has(t) ? b("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, a), u = (t, e, a) => (T(t, e, "access private method"), a), i, y, p, f, _;
let c = class extends w {
  constructor() {
    super(), k(this, i), this.history = [];
  }
  async connectedCallback() {
    super.connectedCallback(), await u(this, i, y).call(this);
  }
  render() {
    var t;
    return ((t = this.history) == null ? void 0 : t.length) > 0 ? l`${this.renderHistory()}` : l`${this.renderEmpty()}`;
  }
  renderEmpty() {
    return l`<umb-empty-state
      ><h2>
        <uui-icon name="usync-logo"></uui-icon
        ><umb-localize key="uSyncHistory_empty"></umb-localize></h2
    ></umb-empty-state>`;
  }
  renderHistory() {
    const t = this.history.map((e) => l`<uui-table-row
        @click=${() => u(this, i, _).call(this, e)}
        class="table"
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
    return l`<umb-body-layout
      ><uui-table>${this.renderTableHead()}${t}</uui-table>
      <div slot="footer-info" class="footer">
        <uui-button
          label="Clear"
          look="primary"
          color="danger"
          @click=${u(this, i, p)}
        ></uui-button></div
    ></umb-body-layout>`;
  }
  renderTableHead() {
    return l`<uui-table-head
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
        return l`<umb-icon name="icon-box"></umb-icon>`;
      case "Export":
        return l`<umb-icon name="icon-shipping"></umb-icon>`;
      default:
        return l`<umb-icon name="icon-document-spreadsheet"></umb-icon>`;
    }
  }
};
i = /* @__PURE__ */ new WeakSet();
y = async function() {
  this.history = (await m.getHistory()).data ?? [];
};
p = function() {
  E(this, {
    headline: this.localize.term("uSyncHistory_clear"),
    content: this.localize.term("uSyncHistory_clearWarning"),
    color: "danger",
    confirmLabel: this.localize.term("uSyncHistory_clear")
  }).then(async () => {
    await u(this, i, f).call(this), await u(this, i, y).call(this);
  }).catch(() => {
  });
};
f = async function() {
  await m.clearHistory();
};
_ = async function(t) {
  return console.log("thing happen", t), S(this, C, {
    data: { item: t }
  }).catch(() => {
  }), null;
};
c.styles = v`
    .footer {
      width: 100%;
      display: flex;
      justify-content: flex-end;
    }

    .table {
      cursor: pointer;
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
  H()
], c.prototype, "history", 2);
c = d([
  g("usync-history-view")
], c);
const U = c;
export {
  U as default,
  c as uSyncHistoryElement
};
//# sourceMappingURL=history-view.element-Dmaa43_J.js.map
