import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import {
  UmbModalContext,
  UmbModalExtensionElement,
} from "@umbraco-cms/backoffice/modal";
import { HistoryModalData, HistoryModalValue } from "./history-modal.token";
import {
  property,
  customElement,
  html,
  css,
} from "@umbraco-cms/backoffice/external/lit";
import { TimeFormatOptions } from "../consts";

@customElement("history-sidebar")
export class HistorySidebarElement
  extends UmbLitElement
  implements UmbModalExtensionElement<HistoryModalData, HistoryModalValue>
{
  @property({ attribute: false })
  modalContext?: UmbModalContext<HistoryModalData, HistoryModalValue>;

  @property({ attribute: false })
  data?: HistoryModalData;

  #handleClose() {
    this.modalContext?.submit();
  }

  render() {
    return html`<umb-body-layout
      ><div slot="header">
        <h3>
          ${this.data?.item.method ?? "History"} @
          ${this.localize.date(this.data?.item.date ?? "", TimeFormatOptions)}
        </h3>
        ${this.data?.item.username}
      </div>
      <div class="layout">
        <usync-results
          .hideActions=${true}
          .hideResultBar=${true}
          .results=${this.data?.item.actions ?? []}
        ></usync-results>
      </div>
      <div slot="actions">
        <uui-button
          id="cancel"
          .label=${this.localize.term("general_close")}
          @click="${this.#handleClose}"
        ></uui-button></div
    ></umb-body-layout>`;
  }
  static styles = css`
    h3 {
      margin: 0px;
    }
  `;
}

export default HistorySidebarElement;
