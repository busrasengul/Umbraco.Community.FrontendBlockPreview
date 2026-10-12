using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.FrontendBlockPreview.Configuration;
using Umbraco.Community.FrontendBlockPreview.Models;

namespace Umbraco.Community.FrontendBlockPreview.Rendering.Razor;

/// <summary>
/// For MVC sites. The backoffice loads /block-preview/{token} from the CMS itself, which renders the block's
/// partial view inside a minimal page carrying the site's stylesheets.
/// </summary>
public class RazorBlockPreviewRenderer : IBlockPreviewRenderer
{
    private readonly IOptionsMonitor<FrontendBlockPreviewOptions> _options;
    private readonly ICompositeViewEngine _viewEngine;
    private readonly IContentTypeService _contentTypeService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedRouter _publishedRouter;

    public RazorBlockPreviewRenderer(
        IOptionsMonitor<FrontendBlockPreviewOptions> options,
        ICompositeViewEngine viewEngine,
        IContentTypeService contentTypeService,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedRouter publishedRouter)
    {
        _options = options;
        _viewEngine = viewEngine;
        _contentTypeService = contentTypeService;
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedRouter = publishedRouter;
    }

    private RazorPreviewOptions RazorOptions => _options.CurrentValue.Razor;

    /// <summary>
    /// Only blocks that have a partial view get the preview.
    /// </summary>
    public IEnumerable<string>? GetSupportedBlockTypes()
        => _contentTypeService.GetAll()
            .Where(contentType => contentType.IsElement)
            .Select(elementType => elementType.Alias)
            .Where(alias => ViewExists(RazorOptions.BlockListViewPath, alias) || ViewExists(RazorOptions.BlockGridViewPath, alias))
            .ToList();

    // Same origin as the backoffice, so no frontend URL is needed
    public string? GetPreviewUrl(IPublishedContent document, string token)
        => $"/{Constants.PreviewPathSegment}/{token}";

    public async Task<IActionResult?> RenderAsync(BlockPreviewContext context)
    {
        object? block = (object?)context.BlockListItem ?? context.BlockGridItem;
        var alias = context.BlockContent?.ContentType.Alias;

        if (block is null || alias is null)
        {
            return null;
        }

        var viewPathFormat = context.BlockGridItem is not null ? RazorOptions.BlockGridViewPath : RazorOptions.BlockListViewPath;
        var viewPath = string.Format(viewPathFormat, alias);

        if (!ViewExists(viewPathFormat, alias))
        {
            return null;
        }

        await AssignPageAsync(context);

        var model = new BlockPreviewViewModel
        {
            Context = context,
            Block = block,
            BlockViewPath = viewPath,
            Stylesheets = RazorOptions.Stylesheets,
            Scripts = RazorOptions.Scripts,
        };

        var result = new ViewResult
        {
            ViewName = RazorOptions.LayoutView,
            ViewData = new ViewDataDictionary<BlockPreviewViewModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model,
            },
        };

        return result;
    }

    /// <summary>
    /// Block views often read the page they're on (Umbraco.AssignedContentItem, CurrentPage). This request
    /// isn't routed to a page, so route it to the previewed page before rendering.
    /// </summary>
    private async Task AssignPageAsync(BlockPreviewContext context)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) || umbracoContext.PublishedRequest is not null)
        {
            return;
        }

        var requestBuilder = await _publishedRouter.CreateRequestAsync(umbracoContext.CleanedUmbracoUrl);
        requestBuilder.SetPublishedContent(context.Document);

        umbracoContext.PublishedRequest = requestBuilder.Build();
    }

    private bool ViewExists(string viewPathFormat, string alias)
        => _viewEngine.GetView(null, string.Format(viewPathFormat, alias), false).Success;
}

public class BlockPreviewViewModel
{
    public required BlockPreviewContext Context { get; init; }

    /// <summary>
    /// The <c>BlockListItem</c> or <c>BlockGridItem</c> passed to the block's partial view.
    /// </summary>
    public required object Block { get; init; }

    public required string BlockViewPath { get; init; }

    public IReadOnlyCollection<string> Stylesheets { get; init; } = [];

    public IReadOnlyCollection<string> Scripts { get; init; } = [];
}
