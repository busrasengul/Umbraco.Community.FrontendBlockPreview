using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.FrontendBlockPreview.Models;
using Umbraco.Community.FrontendBlockPreview.Services;

namespace Umbraco.Community.FrontendBlockPreview.Rendering;

/// <summary>
/// Base for headless frontends. The backoffice loads {frontend}/block-preview/{token}/, the frontend's catch-all
/// route asks the CMS for that path like any other page, and the CMS answers with <see cref="BuildResponse"/>,
/// serialized with the site's normal MVC JSON options.
/// </summary>
public abstract class HeadlessBlockPreviewRendererBase : IBlockPreviewRenderer
{
    private readonly IFrontendUrlResolver _frontendUrlResolver;

    protected HeadlessBlockPreviewRendererBase(IFrontendUrlResolver frontendUrlResolver)
        => _frontendUrlResolver = frontendUrlResolver;

    public virtual IEnumerable<string>? GetSupportedBlockTypes() => null;

    public virtual string? GetPreviewUrl(IPublishedContent document, string token)
    {
        var frontendUrl = _frontendUrlResolver.Resolve(document);

        // Trailing slash: most frontends redirect to it, which would otherwise bounce the iframe
        return string.IsNullOrWhiteSpace(frontendUrl)
            ? null
            : $"{frontendUrl}/{Constants.PreviewPathSegment}/{token}/";
    }

    public virtual Task<IActionResult?> RenderAsync(BlockPreviewContext context)
    {
        var response = BuildResponse(context);

        return Task.FromResult<IActionResult?>(response is null ? null : new OkObjectResult(response));
    }

    /// <summary>
    /// The JSON your frontend expects for a page, containing only the previewed block.
    /// </summary>
    protected abstract object? BuildResponse(BlockPreviewContext context);
}
