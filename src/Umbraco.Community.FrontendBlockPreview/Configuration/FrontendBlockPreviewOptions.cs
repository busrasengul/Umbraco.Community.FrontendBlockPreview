namespace Umbraco.Community.FrontendBlockPreview.Configuration;

/// <summary>
/// Settings from the "FrontendBlockPreview" appsettings section.
/// </summary>
public class FrontendBlockPreviewOptions
{
    /// <summary>
    /// "DeliveryApi", "Razor" or "Custom". With "Custom" the project registers its own renderer.
    /// </summary>
    public string Renderer { get; set; } = Constants.Renderers.Custom;

    /// <summary>
    /// The frontend URL used when no site in <see cref="Sites"/> matches. Leave empty to fall back to the
    /// project's own frontend URL resolver (if it has one).
    /// </summary>
    public string? DefaultFrontendUrl { get; set; }

    /// <summary>
    /// The frontend URL per site, matched by the key of the site's root node (or any ancestor of the page).
    /// </summary>
    public List<FrontendSiteOptions> Sites { get; set; } = [];

    /// <summary>
    /// Block types (element type aliases) that get the preview. When empty, the renderer decides.
    /// </summary>
    public string[] BlockTypes { get; set; } = [];

    /// <summary>
    /// Block types that never get the preview, e.g. blocks the frontend has no component for.
    /// </summary>
    public string[] ExcludedBlockTypes { get; set; } = [];

    public int TokenLifetimeMinutes { get; set; } = 10;

    public RazorPreviewOptions Razor { get; set; } = new();
}

public class FrontendSiteOptions
{
    public Guid RootKey { get; set; }

    public string FrontendUrl { get; set; } = string.Empty;
}

public class RazorPreviewOptions
{
    /// <summary>
    /// Where block list items are rendered from. {0} is the element type alias.
    /// </summary>
    public string BlockListViewPath { get; set; } = "~/Views/Partials/blocklist/Components/{0}.cshtml";

    /// <summary>
    /// Where block grid items are rendered from. {0} is the element type alias.
    /// </summary>
    public string BlockGridViewPath { get; set; } = "~/Views/Partials/blockgrid/Components/{0}.cshtml";

    /// <summary>
    /// The page that wraps the block. Override it to use your site's own head, fonts and scripts.
    /// </summary>
    public string LayoutView { get; set; } = "~/Views/FrontendBlockPreview/BlockPreview.cshtml";

    /// <summary>
    /// Stylesheets added to the default preview page, e.g. "/css/site.css".
    /// </summary>
    public string[] Stylesheets { get; set; } = [];

    /// <summary>
    /// Scripts added to the end of the default preview page.
    /// </summary>
    public string[] Scripts { get; set; } = [];
}
