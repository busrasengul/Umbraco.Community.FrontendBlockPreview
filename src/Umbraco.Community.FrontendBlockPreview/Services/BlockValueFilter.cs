using System.Text.Json.Nodes;

namespace Umbraco.Community.FrontendBlockPreview.Services;

/// <summary>
/// Reduces a stored Block List / Block Grid value to the one top level block being previewed.
/// </summary>
internal static class BlockValueFilter
{
    /// <summary>
    /// Keeps only the top level layout item that is, or contains (in a grid area), <paramref name="contentKey"/>.
    /// </summary>
    /// <returns>The reduced value as JSON, or null when the block isn't in the value.</returns>
    public static string? KeepRootBlock(string storedValue, Guid contentKey, out Guid rootContentKey)
    {
        rootContentKey = Guid.Empty;

        if (JsonNode.Parse(storedValue) is not JsonObject value || value["layout"] is not JsonObject layout)
        {
            return null;
        }

        // The layout is keyed by editor alias ("Umbraco.BlockList" / "Umbraco.BlockGrid")
        foreach (var (editorAlias, items) in layout.ToList())
        {
            if (items is not JsonArray layoutItems)
            {
                continue;
            }

            var rootItem = layoutItems.FirstOrDefault(item => item is not null && Contains(item, contentKey));

            if (rootItem is null)
            {
                continue;
            }

            rootContentKey = GetContentKey(rootItem) ?? contentKey;
            layout[editorAlias] = new JsonArray(rootItem.DeepClone());

            return value.ToJsonString();
        }

        return null;
    }

    private static bool Contains(JsonNode layoutItem, Guid contentKey)
    {
        if (GetContentKey(layoutItem) == contentKey)
        {
            return true;
        }

        // Block Grid: look inside the areas of this item
        return layoutItem["areas"] is JsonArray areas
            && areas.Any(area => area?["items"] is JsonArray items
                && items.Any(item => item is not null && Contains(item, contentKey)));
    }

    private static Guid? GetContentKey(JsonNode layoutItem)
        => Guid.TryParse(layoutItem["contentKey"]?.GetValue<string>(), out var key) ? key : null;
}
