using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.FrontendBlockPreview.Configuration;
using Umbraco.Community.FrontendBlockPreview.Models;
using Umbraco.Community.FrontendBlockPreview.Rendering;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.FrontendBlockPreview.Services;

public interface IBlockPreviewService
{
    /// <summary>
    /// Stores the editor's unsaved block value and returns the URL the backoffice should load.
    /// </summary>
    BlockPreviewTokenResult CreateToken(Guid documentKey, string propertyAlias, Guid contentKey, string? culture, string editorValue);

    /// <summary>
    /// Loads a stored preview and converts it into published blocks, ready for a renderer.
    /// </summary>
    BlockPreviewContext? GetContext(string token);
}

public class BlockPreviewService : IBlockPreviewService
{
    private static readonly string[] SupportedEditors =
    [
        UmbracoConstants.PropertyEditors.Aliases.BlockList,
        UmbracoConstants.PropertyEditors.Aliases.BlockGrid,
    ];

    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IBlockPreviewTokenStore _tokenStore;
    private readonly IBlockPreviewRenderer _renderer;
    private readonly IOptionsMonitor<FrontendBlockPreviewOptions> _options;
    private readonly ILogger<BlockPreviewService> _logger;

    public BlockPreviewService(
        IUmbracoContextFactory umbracoContextFactory,
        IVariationContextAccessor variationContextAccessor,
        PropertyEditorCollection propertyEditors,
        IBlockPreviewTokenStore tokenStore,
        IBlockPreviewRenderer renderer,
        IOptionsMonitor<FrontendBlockPreviewOptions> options,
        ILogger<BlockPreviewService> logger)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _variationContextAccessor = variationContextAccessor;
        _propertyEditors = propertyEditors;
        _tokenStore = tokenStore;
        _renderer = renderer;
        _options = options;
        _logger = logger;
    }

    public BlockPreviewTokenResult CreateToken(Guid documentKey, string propertyAlias, Guid contentKey, string? culture, string editorValue)
    {
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        // The saved draft gives the block its page context, so the page needs saving once
        var document = contextReference.UmbracoContext.Content?.GetById(true, documentKey);

        if (document is null)
        {
            return BlockPreviewTokenResult.Failed("Save the page once before previewing blocks.");
        }

        var propertyType = document.ContentType.GetPropertyType(propertyAlias);

        if (propertyType is null || !SupportedEditors.Contains(propertyType.EditorAlias))
        {
            return BlockPreviewTokenResult.Failed("Preview is only available for blocks directly on the page.");
        }

        string? storedValue;

        try
        {
            storedValue = ConvertToStoredValue(propertyType, editorValue);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not convert the block preview value of {PropertyAlias} on {DocumentKey}", propertyAlias, documentKey);
            return BlockPreviewTokenResult.Failed("This block's values could not be converted for preview.");
        }

        var rootContentKey = Guid.Empty;

        // Only the previewed block (or the grid block containing it) is kept, so renderers just render the value
        var reducedValue = storedValue is null
            ? null
            : BlockValueFilter.KeepRootBlock(storedValue, contentKey, out rootContentKey);

        if (reducedValue is null)
        {
            return BlockPreviewTokenResult.Failed("This block couldn't be found in the property value.");
        }

        var request = new BlockPreviewRequest(documentKey, propertyAlias, contentKey, rootContentKey, culture, reducedValue);

        var token = _tokenStore.Store(request, TimeSpan.FromMinutes(_options.CurrentValue.TokenLifetimeMinutes));

        var url = _renderer.GetPreviewUrl(document, token);

        return string.IsNullOrWhiteSpace(url)
            ? BlockPreviewTokenResult.Failed("No frontend URL is configured for this site.")
            : new BlockPreviewTokenResult(token, url, null);
    }

    public BlockPreviewContext? GetContext(string token)
    {
        var request = _tokenStore.Get(token);

        if (request is null)
        {
            return null;
        }

        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        var contentCache = contextReference.UmbracoContext.Content;
        var document = contentCache?.GetById(true, request.DocumentKey);
        var propertyType = document?.ContentType.GetPropertyType(request.PropertyAlias);

        if (document is null || propertyType is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Culture))
        {
            _variationContextAccessor.VariationContext = new VariationContext(request.Culture);
        }

        // The same value converter Umbraco uses for saved content, so renderers get real blocks
        var inter = propertyType.ConvertSourceToInter(document, request.Value, true);
        var value = propertyType.ConvertInterToObject(document, PropertyCacheLevel.None, inter, true);

        return new BlockPreviewContext
        {
            Request = request,
            Document = document,
            PublishedDocument = contentCache?.GetById(false, request.DocumentKey),
            PropertyType = propertyType,
            Value = value,
        };
    }

    /// <summary>
    /// The backoffice holds values in editor format (e.g. dropdowns as arrays), the value converters expect the
    /// stored format. This runs the same conversion Umbraco runs when the page is saved.
    /// </summary>
    private string? ConvertToStoredValue(IPublishedPropertyType propertyType, string editorValue)
    {
        var configuration = propertyType.DataType.ConfigurationObject;

        var valueEditor = _propertyEditors[propertyType.EditorAlias]?.GetValueEditor(configuration)
            ?? throw new InvalidOperationException($"No property editor found for {propertyType.EditorAlias}");

        return valueEditor.FromEditor(new ContentPropertyData(editorValue, configuration), null)?.ToString();
    }
}
