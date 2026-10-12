using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.FrontendBlockPreview.Models;
using Umbraco.Community.FrontendBlockPreview.Services;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.FrontendBlockPreview.Rendering.DeliveryApi;

/// <summary>
/// For frontends on the Content Delivery API. The preview page is reachable two ways, giving the same JSON as
/// content/item (the page, with the block property holding only the previewed block):
/// <list type="bullet">
///   <item>content/item/block-preview/{token} on the Delivery API, through <see cref="BlockPreviewApiContentPathResolver"/>.
///   Frontends that look content up by path need no code for this.</item>
///   <item>/block-preview/{token} on the CMS, for frontends that look content up by id or another route.</item>
/// </list>
/// </summary>
public class DeliveryApiBlockPreviewRenderer : HeadlessBlockPreviewRendererBase
{
    private readonly IApiContentResponseBuilder _apiContentResponseBuilder;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IOptionsMonitor<JsonOptions> _jsonOptions;

    public DeliveryApiBlockPreviewRenderer(
        IFrontendUrlResolver frontendUrlResolver,
        IApiContentResponseBuilder apiContentResponseBuilder,
        IPublishedValueFallback publishedValueFallback,
        IOptionsMonitor<JsonOptions> jsonOptions)
        : base(frontendUrlResolver)
    {
        _apiContentResponseBuilder = apiContentResponseBuilder;
        _publishedValueFallback = publishedValueFallback;
        _jsonOptions = jsonOptions;
    }

    public override Task<IActionResult?> RenderAsync(BlockPreviewContext context)
    {
        var response = BuildResponse(context);

        // Same serializer settings as the Delivery API itself, so the JSON matches content/item exactly
        var serializerOptions = _jsonOptions.Get(UmbracoConstants.JsonOptionsNames.DeliveryApi).JsonSerializerOptions;

        return Task.FromResult<IActionResult?>(response is null ? null : new JsonResult(response, serializerOptions));
    }

    protected override object? BuildResponse(BlockPreviewContext context)
        => _apiContentResponseBuilder.Build(BlockPreviewPublishedContent.Create(context, _publishedValueFallback));
}
