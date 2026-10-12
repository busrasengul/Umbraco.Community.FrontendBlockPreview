using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.FrontendBlockPreview.Models;

namespace Umbraco.Community.FrontendBlockPreview.Rendering;

/// <summary>
/// Turns a previewed block into something the frontend can show. The package ships one for the Content Delivery
/// API and one for Razor views; headless projects with their own API inherit from
/// <see cref="HeadlessBlockPreviewRendererBase"/>.
/// </summary>
public interface IBlockPreviewRenderer
{
    /// <summary>
    /// The block types (element type aliases) this renderer can show. Null means all of them.
    /// </summary>
    IEnumerable<string>? GetSupportedBlockTypes();

    /// <summary>
    /// The URL the backoffice loads in the preview iframe.
    /// </summary>
    string? GetPreviewUrl(IPublishedContent document, string token);

    /// <summary>
    /// The response for GET /block-preview/{token} on the CMS. Null gives a 404.
    /// </summary>
    Task<IActionResult?> RenderAsync(BlockPreviewContext context);
}
