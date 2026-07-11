import { css, html, customElement, LitElement } from "@umbraco-cms/backoffice/external/lit";

@customElement("casko-robots-txt-configuration-workspace-view")
export class CaskoRobotsTxtConfigurationWorkspaceViewElement extends LitElement {
  override render() {
    return html`
      <uui-box headline="Configuration">
        <dl>
          <dt>Configuration section</dt>
          <dd>RobotsTxt</dd>
          <dt>Public rewrite</dt>
          <dd>/robots.txt</dd>
          <dt>Delivery API</dt>
          <dd>/umbraco/delivery/api/v1/robotstxt</dd>
          <dt>Storage</dt>
          <dd>Umbraco Media folder: Robots Txt</dd>
        </dl>
      </uui-box>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    dl {
      display: grid;
      grid-template-columns: minmax(12rem, 18rem) 1fr;
      gap: var(--uui-size-space-3) var(--uui-size-space-5);
      margin: 0;
    }

    dt {
      font-weight: 700;
    }

    dd {
      margin: 0;
    }
  `;
}

export default CaskoRobotsTxtConfigurationWorkspaceViewElement;
