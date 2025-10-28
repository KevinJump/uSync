import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { HistoryInfo } from "../api";

export type HistoryModalData = {
  item: HistoryInfo;
};

export type HistoryModalValue = {
  myData: string;
};

export const HISTORY_MODAL_TOKEN = new UmbModalToken<
  HistoryModalData,
  HistoryModalValue
>("usync.history.modal", {
  modal: {
    type: "sidebar",
    size: "small",
  },
});
