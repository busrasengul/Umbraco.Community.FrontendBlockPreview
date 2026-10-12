namespace Umbraco.Community.FrontendBlockPreview;

public static class Constants
{
    public const string PackageName = "Umbraco.Community.FrontendBlockPreview";

    public const string ConfigurationSection = "FrontendBlockPreview";

    /// <summary>
    /// The path the preview is served on: /block-preview/{token}/ on the frontend (headless) or the CMS (Razor).
    /// </summary>
    public const string PreviewPathSegment = "block-preview";

    public const string ManagementApiRoute = "frontend-block-preview";

    public const string AppPluginsPath = "/App_Plugins/FrontendBlockPreview";

    public static class Renderers
    {
        /// <summary>The frontend reads the preview through the Content Delivery API.</summary>
        public const string DeliveryApi = "DeliveryApi";

        /// <summary>The CMS renders the block's Razor partial view (MVC sites).</summary>
        public const string Razor = "Razor";

        /// <summary>The project registers its own <see cref="Rendering.IBlockPreviewRenderer"/>.</summary>
        public const string Custom = "Custom";
    }
}
