using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.FrontendBlockPreview.Rendering;
using Umbraco.Community.FrontendBlockPreview.Services;

namespace Umbraco.Community.FrontendBlockPreview.Controllers;

/// <summary>
/// GET /block-preview/{token} on the CMS: requested by a headless frontend like any other page,
/// or loaded straight into the backoffice iframe for Razor sites.
/// </summary>
public class BlockPreviewRenderController : Controller
{
    private readonly IBlockPreviewService _blockPreviewService;
    private readonly IBlockPreviewRenderer _renderer;

    public BlockPreviewRenderController(IBlockPreviewService blockPreviewService, IBlockPreviewRenderer renderer)
    {
        _blockPreviewService = blockPreviewService;
        _renderer = renderer;
    }

    [HttpGet(Constants.PreviewPathSegment + "/{token}")]
    public async Task<IActionResult> Render(string token)
    {
        var context = _blockPreviewService.GetContext(token);

        var result = context is null ? null : await _renderer.RenderAsync(context);

        if (result is null)
        {
            return NotFound();
        }

        // The token is the only thing standing in front of draft content, so keep it out of every cache
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        return result;
    }
}
