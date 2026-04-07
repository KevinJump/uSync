import {
  css,
  customElement,
  html,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { History, HistoryInfo } from "../api";
import { TimeFormatOptions } from "../consts";
import { umbConfirmModal, umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { HISTORY_MODAL_TOKEN } from "../dialogs/history-modal.token";

@customElement("usync-history-view")
export class uSyncHistoryElement extends UmbLitElement {
  @state()
  history: Array<HistoryInfo> = [];

  @state()
  isEnabled = false;

  constructor() {
    super();
  }

  async connectedCallback() {
    super.connectedCallback();

    this.isEnabled = await this.#isEnabled();

    if (!this.isEnabled) return;

    // load
    await this.#loadHistory();
  }

  async #isEnabled() {
    return (await History.historyIsEnabled()).data ?? false;
  }

  async #loadHistory() {
    this.history = (await History.getHistory()).data ?? [];
  }

  #onclick() {
    umbConfirmModal(this, {
      headline: this.localize.term("uSyncHistory_clear"),
      content: this.localize.term("uSyncHistory_clearWarning"),
      color: "danger",
      confirmLabel: this.localize.term("uSyncHistory_clear"),
    })
      .then(async () => {
        await this.#clearHistory();
        await this.#loadHistory();
      })
      .catch(() => {});
  }

  async #clearHistory() {
    await History.clearHistory();
  }

  render() {
    if (!this.isEnabled) return this.renderDisabled();

    if (this.history?.length > 0) return html`${this.renderHistory()}`;
    return html`${this.renderEmpty()}`;
  }

  renderDisabled() {
    return html`<umb-empty-state
      ><h2>
        <uui-icon name="usync-logo"></uui-icon>
      </h2>
      <h3><umb-localize key="uSyncHistory_disabled"></umb-localize></h3
    ></umb-empty-state>`;
  }

  renderEmpty() {
    return html`<umb-empty-state
      ><h2>
        <uui-icon name="usync-logo"></uui-icon
        ><umb-localize key="uSyncHistory_empty"></umb-localize></h2
    ></umb-empty-state>`;
  }

  renderHistory() {
    const items = this.history.map((item) => {
      return html`<uui-table-row
        @click=${() => this.#showDetail(item)}
        class="table"
        ><uui-table-cell>${this.renderIcon(item)}</uui-table-cell
        ><uui-table-cell
          ><umb-localize
            key="uSync_${item.method}"
          ></umb-localize></uui-table-cell
        ><uui-table-cell>${item.changes}/${item.total}</uui-table-cell
        ><uui-table-cell>${item.username}</uui-table-cell>
        <uui-table-cell
          >${this.localize.date(item.date, TimeFormatOptions)}</uui-table-cell
        ></uui-table-row
      >`;
    });

    return html`<umb-body-layout
      ><uui-table>${this.renderTableHead()}${items}</uui-table>
      <div slot="footer-info" class="footer">
        <uui-button
          label="Clear"
          look="primary"
          color="danger"
          @click=${this.#onclick}
        ></uui-button></div
    ></umb-body-layout>`;
  }

  async #showDetail(item: HistoryInfo) {
    umbOpenModal(this, HISTORY_MODAL_TOKEN, {
      data: { item: item },
    }).catch(() => undefined);
  }

  renderTableHead() {
    return html`<uui-table-head
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

  renderIcon(item: HistoryInfo) {
    switch (item.method) {
      case "Import":
        return html`<umb-icon name="icon-box"></umb-icon>`;
      case "Export":
        return html`<umb-icon name="icon-shipping"></umb-icon>`;
      default:
        return html`<umb-icon name="icon-document-spreadsheet"></umb-icon>`;
    }
  }
  static styles = css`
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
}

export default uSyncHistoryElement;
