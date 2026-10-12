/**
 * Frontend block preview helper. Works with any frontend (React, Next.js, Angular, Vue, Nuxt, Svelte,
 * Astro, server-rendered HTML, plain JS).
 *
 * Load it on every page, however your framework adds scripts:
 *   <script src="https://your-cms/App_Plugins/FrontendBlockPreview/frontend-block-preview.js"
 *           data-hide="header, footer, .cookie-banner" defer></script>
 *
 * If your framework injects scripts itself (so data- attributes don't survive), set the options first:
 *   window.FrontendBlockPreview = { hide: 'header, footer, .cookie-banner' };
 *
 * It does nothing unless the page is a /block-preview/ URL inside an iframe. Then it:
 *   - adds the "frontend-block-preview" class to <html>, for your own preview styles
 *   - hides the elements in "hide" (your header, footer, banners, chat widgets)
 *   - tells the backoffice how tall the content is, so the iframe fits the block
 *
 * The only thing the backoffice really needs is this message, if you'd rather write it yourself:
 *   window.parent.postMessage({ type: 'frontend-block-preview:height', height }, '*')
 */
(function () {
  var MESSAGE_TYPE = 'frontend-block-preview:height';

  // Anywhere in the path, so locale or base path prefixes (/en/block-preview/...) still work
  if (window.parent === window || location.pathname.indexOf('/block-preview/') === -1) return;

  // document.currentScript is null when frameworks inject the script, so also look it up by name
  var script =
    document.currentScript || document.querySelector('script[src*="frontend-block-preview"]');
  var options = window.FrontendBlockPreview || {};
  var hide = options.hide || (script && script.getAttribute('data-hide')) || '';

  document.documentElement.classList.add('frontend-block-preview');

  var style = document.createElement('style');
  style.textContent =
    'html.frontend-block-preview, html.frontend-block-preview body { min-height: 0 !important; }' +
    (hide ? hide + ' { display: none !important; }' : '');
  document.head.appendChild(style);

  var lastHeight = 0;

  function postHeight() {
    var height = Math.ceil(document.body.scrollHeight);
    if (height === lastHeight) return;
    lastHeight = height;
    window.parent.postMessage({ type: MESSAGE_TYPE, height: height }, '*');
  }

  function start() {
    // Client-rendered apps fill the page after load, so keep watching rather than measuring once
    new ResizeObserver(postHeight).observe(document.body);
    postHeight();
  }

  if (document.body) {
    start();
  } else {
    document.addEventListener('DOMContentLoaded', start);
  }
})();
