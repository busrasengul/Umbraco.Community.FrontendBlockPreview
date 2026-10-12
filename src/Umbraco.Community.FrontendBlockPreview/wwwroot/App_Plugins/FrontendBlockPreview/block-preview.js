import { LitElement as S, nothing as f, html as h, css as A, property as u, state as d, query as L, customElement as U } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin as H } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT as N } from "@umbraco-cms/backoffice/auth";
import { UMB_DOCUMENT_WORKSPACE_CONTEXT as R } from "@umbraco-cms/backoffice/document";
import { UMB_PROPERTY_CONTEXT as K, UMB_PROPERTY_DATASET_CONTEXT as z } from "@umbraco-cms/backoffice/property";
var D = Object.defineProperty, F = Object.getOwnPropertyDescriptor, M = (t) => {
  throw TypeError(t);
}, o = (t, e, r, n) => {
  for (var l = n > 1 ? void 0 : n ? F(e, r) : e, _ = t.length - 1, m; _ >= 0; _--)
    (m = t[_]) && (l = (n ? m(e, r, l) : m(l)) || l);
  return n && l && D(e, r, l), l;
}, x = (t, e, r) => e.has(t) || M("Cannot " + r), s = (t, e, r) => (x(t, e, "read from private field"), e.get(t)), p = (t, e, r) => e.has(t) ? M("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, r), g = (t, e, r, n) => (x(t, e, "write to private field"), e.set(t, r), r), c = (t, e, r) => (x(t, e, "access private method"), r), b, w, v, E, y, k, a, C, T, O, $, P, W, B;
const G = "frontend-block-preview:height", I = "/umbraco/management/api/v1/frontend-block-preview/token", V = {
  desktop: 1440,
  mobile: 390
};
let i = class extends H(S) {
  constructor() {
    super(), p(this, a), this._isTopLevel = !1, this._open = !1, this._loading = !1, this._frameHeight = 300, this._containerWidth = 0, this._viewport = "desktop", p(this, b), p(this, w), p(this, v), p(this, E), p(this, y), p(this, k, new ResizeObserver((t) => {
      this._containerWidth = t[0].contentRect.width;
    })), p(this, T, (t) => {
      t.source !== this._iframe?.contentWindow || t.data?.type !== G || (this._frameHeight = Math.max(50, Math.ceil(t.data.height)));
    }), this.consumeContext(R, (t) => {
      g(this, w, t), g(this, b, t?.getUnique() ?? void 0), c(this, a, C).call(this);
    }), this.consumeContext(K, (t) => {
      g(this, v, t), c(this, a, C).call(this);
    }), this.consumeContext(z, (t) => {
      g(this, E, t);
    });
  }
  connectedCallback() {
    super.connectedCallback(), window.addEventListener("message", s(this, T));
  }
  disconnectedCallback() {
    super.disconnectedCallback(), window.removeEventListener("message", s(this, T)), s(this, k).disconnect(), window.clearTimeout(s(this, y));
  }
  updated(t) {
    const e = this.renderRoot.querySelector(".stage");
    e && s(this, k).observe(e), this._open && (t.has("content") || t.has("settings")) && (window.clearTimeout(s(this, y)), g(this, y, window.setTimeout(() => c(this, a, P).call(this), 800)));
  }
  render() {
    return h`
      <div class="header">
        <umb-icon name=${this.icon ?? "icon-document"}></umb-icon>
        <a class="label" href=${this.config?.showContentEdit && this.config.editContentPath || f}>${this.label}</a>
        ${this.unpublished ? h`<uui-tag look="secondary">Unpublished</uui-tag>` : f}
        <span class="spacer"></span>
        ${c(this, a, B).call(this)}
      </div>
      ${this._open && this._isTopLevel ? h`
            <div class="stage">
              ${this._loading && !this._url ? h`<uui-loader-bar></uui-loader-bar>` : f}
              ${this._error ? h`<p class="error">${this._error}</p>` : c(this, a, W).call(this)}
            </div>
          ` : f}
      <!-- Block Grid only: keeps the block's areas editable. Unknown (and empty) in a Block List. -->
      <umb-block-grid-areas-container slot="areas"></umb-block-grid-areas-container>
    `;
  }
};
b = /* @__PURE__ */ new WeakMap();
w = /* @__PURE__ */ new WeakMap();
v = /* @__PURE__ */ new WeakMap();
E = /* @__PURE__ */ new WeakMap();
y = /* @__PURE__ */ new WeakMap();
k = /* @__PURE__ */ new WeakMap();
a = /* @__PURE__ */ new WeakSet();
C = async function() {
  const t = s(this, v)?.getAlias();
  if (!s(this, w) || !t) return;
  const e = await s(this, w).structure.getPropertyStructureByAlias(t);
  this._isTopLevel = !!e;
};
T = /* @__PURE__ */ new WeakMap();
O = function(t) {
  t.stopPropagation(), this._open = !this._open, this._open && c(this, a, P).call(this);
};
$ = function(t, e) {
  t.stopPropagation(), this._viewport = e;
};
P = async function() {
  const t = s(this, v)?.getAlias(), e = this.contentKey ?? this.layout?.contentKey, r = [
    !s(this, b) && "document key",
    !t && "property alias",
    !e && "block key"
  ].filter(Boolean);
  if (r.length) {
    this._error = `Could not work out which page and property this block belongs to (missing ${r.join(", ")}).`;
    return;
  }
  this._loading = !0, this._error = void 0;
  try {
    const l = await (await this.getContext(N))?.getLatestToken(), _ = await fetch(I, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${l}`,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        documentKey: s(this, b),
        propertyAlias: t,
        contentKey: e,
        culture: s(this, E)?.getVariantId?.()?.culture ?? null,
        // The whole property value, including changes that haven't been saved yet
        value: s(this, v)?.getValue()
      })
    }), m = await _.json().catch(() => ({}));
    if (!_.ok) {
      this._error = m.error ?? `Preview failed (HTTP ${_.status})`;
      return;
    }
    this._url = m.url;
  } catch (n) {
    this._error = n instanceof Error ? n.message : String(n);
  } finally {
    this._loading = !1;
  }
};
W = function() {
  if (!this._url) return f;
  const t = V[this._viewport], e = this._containerWidth ? Math.min(1, this._containerWidth / t) : 1;
  return h`
      <div class="frame-wrapper" style="height: ${this._frameHeight * e}px; width: ${t * e}px">
        <iframe
          src=${this._url}
          title="Frontend preview of ${this.label ?? "block"}"
          style="width: ${t}px; height: ${this._frameHeight}px; transform: scale(${e})"></iframe>
      </div>
    `;
};
B = function() {
  return this._isTopLevel ? h`
      ${this._open ? h`
            <uui-button-group>
              <uui-button compact look=${this._viewport === "desktop" ? "primary" : "secondary"}
                @click=${(t) => c(this, a, $).call(this, t, "desktop")}>Desktop</uui-button>
              <uui-button compact look=${this._viewport === "mobile" ? "primary" : "secondary"}
                @click=${(t) => c(this, a, $).call(this, t, "mobile")}>Mobile</uui-button>
            </uui-button-group>
          ` : f}
      <uui-button compact look="outline" @click=${c(this, a, O)}>
        ${this._open ? "Hide preview" : "Preview"}
      </uui-button>
    ` : f;
};
i.styles = A`
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
o([
  u({ attribute: !1 })
], i.prototype, "content", 2);
o([
  u({ attribute: !1 })
], i.prototype, "settings", 2);
o([
  u({ attribute: !1 })
], i.prototype, "config", 2);
o([
  u({ attribute: !1 })
], i.prototype, "contentKey", 2);
o([
  u({ attribute: !1 })
], i.prototype, "layout", 2);
o([
  u()
], i.prototype, "label", 2);
o([
  u()
], i.prototype, "icon", 2);
o([
  u({ type: Boolean })
], i.prototype, "unpublished", 2);
o([
  d()
], i.prototype, "_isTopLevel", 2);
o([
  d()
], i.prototype, "_open", 2);
o([
  d()
], i.prototype, "_loading", 2);
o([
  d()
], i.prototype, "_error", 2);
o([
  d()
], i.prototype, "_url", 2);
o([
  d()
], i.prototype, "_frameHeight", 2);
o([
  d()
], i.prototype, "_containerWidth", 2);
o([
  d()
], i.prototype, "_viewport", 2);
o([
  L("iframe")
], i.prototype, "_iframe", 2);
i = o([
  U("frontend-block-preview-view")
], i);
const Q = i;
export {
  i as FrontendBlockPreviewElement,
  Q as default
};
//# sourceMappingURL=block-preview.js.map
