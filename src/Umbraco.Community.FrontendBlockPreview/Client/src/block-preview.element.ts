import { LitElement, html, css, customElement, nothing, property, state, query } from '@umbraco-cms/backoffice/external/lit';
import type { PropertyValues } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UMB_PROPERTY_CONTEXT, UMB_PROPERTY_DATASET_CONTEXT } from '@umbraco-cms/backoffice/property';
import type { UmbBlockDataType, UmbBlockLayoutBaseModel } from '@umbraco-cms/backoffice/block';
import type {
  UmbBlockEditorCustomViewConfiguration,
  UmbBlockEditorCustomViewElement,
} from '@umbraco-cms/backoffice/block-custom-view';

// Posted by the frontend (or frontend-block-preview.js) with its content height
const HEIGHT_MESSAGE = 'frontend-block-preview:height';
const TOKEN_ENDPOINT = '/umbraco/management/api/v1/frontend-block-preview/token';

const VIEWPORTS = {
  desktop: 1440,
  mobile: 390,
} as const;

type Viewport = keyof typeof VIEWPORTS;

/**
 * Block List / Block Grid custom view that shows the block rendered by the real frontend.
 * The unsaved property value is posted to the CMS, which returns the URL that renders it.
 */
@customElement('frontend-block-preview-view')
export class FrontendBlockPreviewElement extends UmbElementMixin(LitElement) implements UmbBlockEditorCustomViewElement {
  @property({ attribute: false }) content?: UmbBlockDataType;
  @property({ attribute: false }) settings?: UmbBlockDataType;
  @property({ attribute: false }) config?: UmbBlockEditorCustomViewConfiguration;
  @property({ attribute: false }) contentKey?: string;
  @property({ attribute: false }) layout?: UmbBlockLayoutBaseModel;
  @property() label?: string;
  @property() icon?: string;
  @property({ type: Boolean }) unpublished?: boolean;

  @state() private _isTopLevel = false;
  @state() private _open = false;
  @state() private _loading = false;
  @state() private _error?: string;
  @state() private _url?: string;
  @state() private _frameHeight = 300;
  @state() private _containerWidth = 0;
  @state() private _viewport: Viewport = 'desktop';

  @query('iframe') private _iframe?: HTMLIFrameElement;

  #documentKey?: string;
  #documentWorkspace?: typeof UMB_DOCUMENT_WORKSPACE_CONTEXT.TYPE;
  #propertyContext?: typeof UMB_PROPERTY_CONTEXT.TYPE;
  #datasetContext?: typeof UMB_PROPERTY_DATASET_CONTEXT.TYPE;
  #refreshTimer?: number;
  #resizeObserver = new ResizeObserver(entries => {
    this._containerWidth = entries[0].contentRect.width;
  });

  constructor() {
    super();

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, context => {
      this.#documentWorkspace = context;
      this.#documentKey = context?.getUnique() ?? undefined;
      this.#checkTopLevel();
    });

    this.consumeContext(UMB_PROPERTY_CONTEXT, context => {
      this.#propertyContext = context;
      this.#checkTopLevel();
    });

    this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, context => {
      this.#datasetContext = context;
    });
  }

  connectedCallback() {
    super.connectedCallback();
    window.addEventListener('message', this.#onMessage);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.removeEventListener('message', this.#onMessage);
    this.#resizeObserver.disconnect();
    window.clearTimeout(this.#refreshTimer);
  }

  protected updated(changed: PropertyValues) {
    const stage = this.renderRoot.querySelector('.stage');
    if (stage) this.#resizeObserver.observe(stage);

    // Re-render while the editor types, debounced so we don't hammer the frontend
    if (this._open && (changed.has('content') || changed.has('settings'))) {
      window.clearTimeout(this.#refreshTimer);
      this.#refreshTimer = window.setTimeout(() => this.#loadPreview(), 800);
    }
  }

  /**
   * Block types are often allowed inside other blocks too. Those nested copies have no page of their own,
   * so only offer the preview when the block's property sits directly on the document.
   */
  async #checkTopLevel() {
    const propertyAlias = this.#propertyContext?.getAlias();
    if (!this.#documentWorkspace || !propertyAlias) return;

    const propertyType = await this.#documentWorkspace.structure.getPropertyStructureByAlias(propertyAlias);
    this._isTopLevel = !!propertyType;
  }

  #onMessage = (event: MessageEvent) => {
    if (event.source !== this._iframe?.contentWindow || event.data?.type !== HEIGHT_MESSAGE) return;
    this._frameHeight = Math.max(50, Math.ceil(event.data.height));
  };

  #toggle(event: Event) {
    event.stopPropagation();
    this._open = !this._open;
    if (this._open) this.#loadPreview();
  }

  #setViewport(event: Event, viewport: Viewport) {
    event.stopPropagation();
    this._viewport = viewport;
  }

  async #loadPreview() {
    const propertyAlias = this.#propertyContext?.getAlias();
    // The block editors don't fill contentKey for custom views, but the layout carries it
    const contentKey = this.contentKey ?? this.layout?.contentKey;

    const missing = [
      !this.#documentKey && 'document key',
      !propertyAlias && 'property alias',
      !contentKey && 'block key',
    ].filter(Boolean);

    if (missing.length) {
      this._error = `Could not work out which page and property this block belongs to (missing ${missing.join(', ')}).`;
      return;
    }

    this._loading = true;
    this._error = undefined;

    try {
      const authContext = await this.getContext(UMB_AUTH_CONTEXT);
      const token = await authContext?.getLatestToken();

      const response = await fetch(TOKEN_ENDPOINT, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          documentKey: this.#documentKey,
          propertyAlias,
          contentKey,
          culture: this.#datasetContext?.getVariantId?.()?.culture ?? null,
          // The whole property value, including changes that haven't been saved yet
          value: this.#propertyContext?.getValue(),
        }),
      });

      const result = await response.json().catch(() => ({}));

      if (!response.ok) {
        this._error = result.error ?? `Preview failed (HTTP ${response.status})`;
        return;
      }

      this._url = result.url;
    } catch (error) {
      this._error = error instanceof Error ? error.message : String(error);
    } finally {
      this._loading = false;
    }
  }

  #renderFrame() {
    if (!this._url) return nothing;

    // Render at a real device width and scale it down to fit the block card
    const frameWidth = VIEWPORTS[this._viewport];
    const scale = this._containerWidth ? Math.min(1, this._containerWidth / frameWidth) : 1;

    return html`
      <div class="frame-wrapper" style="height: ${this._frameHeight * scale}px; width: ${frameWidth * scale}px">
        <iframe
          src=${this._url}
          title="Frontend preview of ${this.label ?? 'block'}"
          style="width: ${frameWidth}px; height: ${this._frameHeight}px; transform: scale(${scale})"></iframe>
      </div>
    `;
  }

  #renderToolbar() {
    if (!this._isTopLevel) return nothing;

    return html`
      ${this._open
        ? html`
            <uui-button-group>
              <uui-button compact look=${this._viewport === 'desktop' ? 'primary' : 'secondary'}
                @click=${(e: Event) => this.#setViewport(e, 'desktop')}>Desktop</uui-button>
              <uui-button compact look=${this._viewport === 'mobile' ? 'primary' : 'secondary'}
                @click=${(e: Event) => this.#setViewport(e, 'mobile')}>Mobile</uui-button>
            </uui-button-group>
          `
        : nothing}
      <uui-button compact look="outline" @click=${this.#toggle}>
        ${this._open ? 'Hide preview' : 'Preview'}
      </uui-button>
    `;
  }

  render() {
    return html`
      <div class="header">
        <umb-icon name=${this.icon ?? 'icon-document'}></umb-icon>
        <a class="label" href=${(this.config?.showContentEdit && this.config.editContentPath) || nothing}>${this.label}</a>
        ${this.unpublished ? html`<uui-tag look="secondary">Unpublished</uui-tag>` : nothing}
        <span class="spacer"></span>
        ${this.#renderToolbar()}
      </div>
      ${this._open && this._isTopLevel
        ? html`
            <div class="stage">
              ${this._loading && !this._url ? html`<uui-loader-bar></uui-loader-bar>` : nothing}
              ${this._error ? html`<p class="error">${this._error}</p>` : this.#renderFrame()}
            </div>
          `
        : nothing}
      <!-- Block Grid only: keeps the block's areas editable. Unknown (and empty) in a Block List. -->
      <umb-block-grid-areas-container slot="areas"></umb-block-grid-areas-container>
    `;
  }

  static styles = css`
    :host {
      display: block;
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface);
      min-height: 50px;
    }

    .header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      /* Leave room for Umbraco's edit/copy/delete action bar over the top right corner */
      padding-right: 120px;
    }

    .label {
      font-weight: 700;
      color: var(--uui-color-text);
      text-decoration: none;
    }

    .label:hover {
      text-decoration: underline;
    }

    .spacer {
      flex: 1;
    }

    .stage {
      border-top: 1px solid var(--uui-color-border);
      padding: var(--uui-size-space-3);
      overflow: hidden;
    }

    .frame-wrapper {
      margin: 0 auto;
      overflow: hidden;
    }

    iframe {
      border: 0;
      display: block;
      transform-origin: top left;
      background: #fff;
    }

    .error {
      margin: 0;
      color: var(--uui-color-danger);
    }
  `;
}

export default FrontendBlockPreviewElement;

declare global {
  interface HTMLElementTagNameMap {
    'frontend-block-preview-view': FrontendBlockPreviewElement;
  }
}
