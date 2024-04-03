import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { LitElement, css, customElement, html } from "@umbraco-cms/backoffice/external/lit";

import logo from "../../../img/usync-complete.png";

@customElement('usync-addons-view')
export class uSyncAddOnsElement extends UmbElementMixin(LitElement) {

    render() {
        return html`
            <umb-body-layout>
                <div class="addon-view">
                    <div>
                        <div class="header">
                            <img src=${logo}>
                            <h1>uSync Complete</h1>
                            <p>
                                uSync Complete gives you total control over your Umbraco settings and content.
                            </p>
                        </div>

                        <div class="logos">
                            <div class="logo">
                                <uui-icon name="icon-shift"></uui-icon>
                                <h4>Publish</h4>
                            </div>
                            <div class="logo">
                                <uui-icon name="icon-notepad"></uui-icon>
                                <h4>content</h4>
                            </div>
                            <div class="logo">
                                <uui-icon name="icon-compress"></uui-icon>
                                <h4>export</h4>
                            </div>
                            <div class="logo">
                                <uui-icon name="icon-connection"></uui-icon>
                                <h4>snapshot</h4>
                            </div>
                            <div class="logo">
                                <uui-icon name="icon-undo"></uui-icon>
                                <h4>restore</h4>
                            </div>
                            <div class="logo">
                                <uui-icon name="icon-operator"></uui-icon>
                                <h4>people</h4>
                            </div>
                        </div>

                        <div class="cta">
                            <uui-button 
                                href="https://jumoo.co.uk/uSync/complete/"
                                target="_blank"
                                color="positive" 
                                look="primary" 
                                label="Find out more"></uui-button>
                        </div>
                    </div>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = css`

        umb-body-layout {
            background: linear-gradient(#e3e3f1,#f6f4f4);
        }

        .addon-view {
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100%;
        }

        .header, .cta {
            display: flex;
            flex-direction: column;
            align-items: center;            
        }

        .header {
            font-size: larger;
            margin-bottom: 20px;
        }

        .logos {
            display: flex;
            justify-content: space-between;
            font-size: 20pt;
        }

        .logo {
            display: flex;
            flex-direction: column;
            align-items: center;
            margin: 10px 30px;
            color: #555;
        }

        .cta {
            margin-bottom: 40px;
        }

        uui-button {
            font-size: 15pt;
        }
    `;
}

export default uSyncAddOnsElement;