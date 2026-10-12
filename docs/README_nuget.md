# Umbraco.Community.FrontendBlockPreview

Preview Block List and Block Grid blocks in the Umbraco backoffice, **rendered by your real frontend**, with the editor's **unsaved** changes.

![Every block previewed by the frontend](https://raw.githubusercontent.com/busrasengul/Umbraco.Community.FrontendBlockPreview/main/docs/screenshots/headless-all-blocks.png)

![Unsaved changes shown straight away in the frontend preview](https://raw.githubusercontent.com/busrasengul/Umbraco.Community.FrontendBlockPreview/main/docs/screenshots/live-edit.png)

- **Headless on the Content Delivery API:** `"Renderer": "DeliveryApi"`, no CMS code.
- **Headless on your own API:** `"Renderer": "Custom"` plus one small class.
- **MVC:** `"Renderer": "Razor"` renders each block's partial view.
- **Any frontend:** React, Next.js, Angular, Vue, Nuxt, SvelteKit, Astro or plain JavaScript. Add a catch-all route (you probably have one) and one script tag.
- **Multisite:** a frontend URL per site, or your own resolver.
- Desktop and mobile widths, live updates while typing.

```json
"FrontendBlockPreview": {
  "Renderer": "DeliveryApi",
  "DefaultFrontendUrl": "https://www.example.com"
}
```

```html
<script src="https://your-cms/App_Plugins/FrontendBlockPreview/frontend-block-preview.js"
        data-hide="header, footer" defer></script>
```

See the [documentation](https://github.com/busrasengul/Umbraco.Community.FrontendBlockPreview#readme) for every framework, multisite and settings.
