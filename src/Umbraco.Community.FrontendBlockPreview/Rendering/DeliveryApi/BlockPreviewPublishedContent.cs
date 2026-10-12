using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.FrontendBlockPreview.Models;

namespace Umbraco.Community.FrontendBlockPreview.Rendering.DeliveryApi;

/// <summary>
/// The page with one property swapped for the previewed value. Everything else is the real page, so the
/// frontend renders it with its normal page template.
/// </summary>
internal class BlockPreviewPublishedContent : PublishedContentWrapped
{
    private readonly IPublishedProperty _previewProperty;

    public BlockPreviewPublishedContent(
        IPublishedContent content,
        IPublishedValueFallback publishedValueFallback,
        IPublishedPropertyType propertyType,
        string previewValue)
        : base(content, publishedValueFallback)
        => _previewProperty = new BlockPreviewPublishedProperty(propertyType, this, previewValue);

    /// <summary>
    /// The published page when there is one, so the Delivery API can build its route; the draft for new pages.
    /// </summary>
    public static BlockPreviewPublishedContent Create(BlockPreviewContext context, IPublishedValueFallback publishedValueFallback)
        => new(context.PublishedDocument ?? context.Document, publishedValueFallback, context.PropertyType, context.Request.Value);

    public override IEnumerable<IPublishedProperty> Properties
        => base.Properties.Select(property => property.Alias == _previewProperty.Alias ? _previewProperty : property);

    public override IPublishedProperty? GetProperty(string alias)
        => alias == _previewProperty.Alias ? _previewProperty : base.GetProperty(alias);
}

/// <summary>
/// A property whose value comes from the preview instead of the content cache.
/// </summary>
internal class BlockPreviewPublishedProperty : IPublishedProperty
{
    private readonly IPublishedElement _owner;
    private readonly string _sourceValue;
    private readonly Lazy<object?> _inter;

    public BlockPreviewPublishedProperty(IPublishedPropertyType propertyType, IPublishedElement owner, string sourceValue)
    {
        PropertyType = propertyType;
        _owner = owner;
        _sourceValue = sourceValue;
        _inter = new Lazy<object?>(() => PropertyType.ConvertSourceToInter(_owner, _sourceValue, true));
    }

    public IPublishedPropertyType PropertyType { get; }

    public string Alias => PropertyType.Alias;

    public bool HasValue(string? culture = null, string? segment = null) => true;

    public object? GetSourceValue(string? culture = null, string? segment = null) => _sourceValue;

    public object? GetValue(string? culture = null, string? segment = null)
        => PropertyType.ConvertInterToObject(_owner, PropertyCacheLevel.None, _inter.Value, true);

    public object? GetDeliveryApiValue(bool expanding, string? culture = null, string? segment = null)
        => PropertyType.ConvertInterToDeliveryApiObject(_owner, PropertyCacheLevel.None, _inter.Value, true, expanding);
}
