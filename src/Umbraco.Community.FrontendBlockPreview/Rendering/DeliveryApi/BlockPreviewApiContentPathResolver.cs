using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Community.FrontendBlockPreview.Services;

namespace Umbraco.Community.FrontendBlockPreview.Rendering.DeliveryApi;

/// <summary>
/// Wraps Umbraco's path resolver so content/item/block-preview/{token} returns the previewed page.
/// Every other path goes to Umbraco's resolver untouched.
/// </summary>
public partial class BlockPreviewApiContentPathResolver : IApiContentPathResolver
{
    private readonly IApiContentPathResolver _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public BlockPreviewApiContentPathResolver(
        IApiContentPathResolver inner,
        IHttpContextAccessor httpContextAccessor,
        IPublishedValueFallback publishedValueFallback)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
        _publishedValueFallback = publishedValueFallback;
    }

    public IPublishedContent? ResolveContentPath(string path)
    {
        var match = PreviewPathRegex().Match(path);

        if (!match.Success)
        {
            return _inner.ResolveContentPath(path);
        }

        // The path resolver is a singleton, the preview service lives in the request scope
        var previewService = _httpContextAccessor.HttpContext?.RequestServices.GetService<IBlockPreviewService>();
        var context = previewService?.GetContext(match.Groups["token"].Value);

        if (context is null)
        {
            return null;
        }

        return BlockPreviewPublishedContent.Create(context, _publishedValueFallback);
    }

    [GeneratedRegex($"^/?{Constants.PreviewPathSegment}/(?<token>[a-f0-9]{{32}})/?$", RegexOptions.IgnoreCase)]
    private static partial Regex PreviewPathRegex();
}
