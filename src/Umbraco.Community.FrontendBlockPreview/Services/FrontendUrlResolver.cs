using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.FrontendBlockPreview.Configuration;

namespace Umbraco.Community.FrontendBlockPreview.Services;

/// <summary>
/// Works out which frontend renders the preview for a page. Multisite projects resolve a different
/// frontend per site.
/// </summary>
public interface IFrontendUrlResolver
{
    string? Resolve(IPublishedContent document);
}

/// <summary>
/// Resolves the frontend from appsettings: the first site in "Sites" whose root key is the page or one of its
/// ancestors, then "DefaultFrontendUrl", then <see cref="ResolveFallback"/>. Inherit from this to plug in a
/// project's existing setting (e.g. a frontend domain on a site settings node) while keeping the appsettings
/// override for local development.
/// </summary>
public class ConfigurationFrontendUrlResolver : IFrontendUrlResolver
{
    private readonly IOptionsMonitor<FrontendBlockPreviewOptions> _options;
    private readonly IIdKeyMap _idKeyMap;

    public ConfigurationFrontendUrlResolver(IOptionsMonitor<FrontendBlockPreviewOptions> options, IIdKeyMap idKeyMap)
    {
        _options = options;
        _idKeyMap = idKeyMap;
    }

    public string? Resolve(IPublishedContent document)
    {
        var options = _options.CurrentValue;

        if (options.Sites.Count > 0)
        {
            var ancestorKeys = GetAncestorOrSelfKeys(document);

            var site = options.Sites.FirstOrDefault(s => ancestorKeys.Contains(s.RootKey));

            if (site is not null && !string.IsNullOrWhiteSpace(site.FrontendUrl))
            {
                return site.FrontendUrl.TrimEnd('/');
            }
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultFrontendUrl))
        {
            return options.DefaultFrontendUrl.TrimEnd('/');
        }

        return ResolveFallback(document)?.TrimEnd('/');
    }

    /// <summary>
    /// Used when appsettings doesn't name a frontend for the page.
    /// </summary>
    protected virtual string? ResolveFallback(IPublishedContent document) => null;

    private HashSet<Guid> GetAncestorOrSelfKeys(IPublishedContent document)
    {
        var keys = new HashSet<Guid> { document.Key };

        // Path is "-1,1061,1062,..." from the root down to the page itself
        foreach (var segment in document.Path.Split(','))
        {
            if (int.TryParse(segment, out var id) && id > 0)
            {
                var key = _idKeyMap.GetKeyForId(id, UmbracoObjectTypes.Document);

                if (key.Success)
                {
                    keys.Add(key.Result);
                }
            }
        }

        return keys;
    }
}
