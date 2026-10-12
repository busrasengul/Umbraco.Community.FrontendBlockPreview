using Umbraco.Cms.Core.Cache;
using Umbraco.Community.FrontendBlockPreview.Models;
using Umbraco.Extensions;

namespace Umbraco.Community.FrontendBlockPreview.Services;

/// <summary>
/// Keeps preview requests between the backoffice creating them and the frontend reading them.
/// The default keeps them in the runtime cache of this server; replace it with a distributed store
/// when the CMS runs on more than one instance.
/// </summary>
public interface IBlockPreviewTokenStore
{
    string Store(BlockPreviewRequest request, TimeSpan lifetime);

    BlockPreviewRequest? Get(string token);
}

public class RuntimeCacheBlockPreviewTokenStore : IBlockPreviewTokenStore
{
    private const string CacheKeyPrefix = "FrontendBlockPreview_";

    private readonly IAppPolicyCache _runtimeCache;

    public RuntimeCacheBlockPreviewTokenStore(AppCaches appCaches)
        => _runtimeCache = appCaches.RuntimeCache;

    public string Store(BlockPreviewRequest request, TimeSpan lifetime)
    {
        // 128 random bits, lower case so frontends that lower-case URLs don't break it
        var token = Guid.NewGuid().ToString("N");

        _runtimeCache.Insert(CacheKeyPrefix + token, () => request, lifetime);

        return token;
    }

    public BlockPreviewRequest? Get(string token)
        => _runtimeCache.GetCacheItem<BlockPreviewRequest>(CacheKeyPrefix + token);
}
