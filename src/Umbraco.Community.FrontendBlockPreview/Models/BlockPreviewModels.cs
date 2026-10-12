using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Community.FrontendBlockPreview.Models;

/// <summary>
/// What is stored behind a preview token.
/// </summary>
/// <param name="DocumentKey">The page being edited.</param>
/// <param name="PropertyAlias">The Block List or Block Grid property on that page.</param>
/// <param name="ContentKey">The block the editor asked to preview.</param>
/// <param name="RootContentKey">The top level block containing it (the same block unless it sits in a grid area).</param>
/// <param name="Culture">The culture being edited, for variant pages.</param>
/// <param name="Value">The property value in stored format, reduced to the top level block.</param>
public record BlockPreviewRequest(
    Guid DocumentKey,
    string PropertyAlias,
    Guid ContentKey,
    Guid RootContentKey,
    string? Culture,
    string Value);

public record BlockPreviewTokenResult(string? Token, string? Url, string? Error)
{
    public static BlockPreviewTokenResult Failed(string error) => new(null, null, error);
}

/// <summary>
/// Everything a renderer needs to render the previewed block.
/// </summary>
public class BlockPreviewContext
{
    public required BlockPreviewRequest Request { get; init; }

    /// <summary>
    /// The saved draft of the page.
    /// </summary>
    public required IPublishedContent Document { get; init; }

    /// <summary>
    /// The published version of the page, when it has one.
    /// </summary>
    public IPublishedContent? PublishedDocument { get; init; }

    public required IPublishedPropertyType PropertyType { get; init; }

    /// <summary>
    /// The converted property value (<see cref="BlockListModel"/> or <see cref="BlockGridModel"/>)
    /// containing only the top level block.
    /// </summary>
    public object? Value { get; init; }

    public BlockListItem? BlockListItem => (Value as BlockListModel)?.FirstOrDefault();

    public BlockGridItem? BlockGridItem => (Value as BlockGridModel)?.FirstOrDefault();

    /// <summary>
    /// The top level block, whichever editor it comes from.
    /// </summary>
    public IPublishedElement? BlockContent => BlockListItem?.Content ?? BlockGridItem?.Content;
}
