using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.FrontendBlockPreview.Configuration;
using Umbraco.Community.FrontendBlockPreview.Rendering;

namespace Umbraco.Community.FrontendBlockPreview.Composing;

/// <summary>
/// Registers the block custom view from code, so the block types it applies to come from the renderer
/// (e.g. only blocks with a Razor view or a headless component) and appsettings, not a static JSON file.
/// </summary>
public class FrontendBlockPreviewManifestReader : IPackageManifestReader
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<FrontendBlockPreviewOptions> _options;
    private readonly ILogger<FrontendBlockPreviewManifestReader> _logger;

    public FrontendBlockPreviewManifestReader(
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<FrontendBlockPreviewOptions> options,
        ILogger<FrontendBlockPreviewManifestReader> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var renderer = scope.ServiceProvider.GetService<IBlockPreviewRenderer>();

        if (renderer is null)
        {
            _logger.LogWarning(
                "Frontend block preview is installed but no renderer is registered. Set {Section}:Renderer to DeliveryApi or Razor, or register your own IBlockPreviewRenderer.",
                Constants.ConfigurationSection);

            return Task.FromResult(Enumerable.Empty<PackageManifest>());
        }

        var options = _options.CurrentValue;

        var blockTypes = options.BlockTypes.Length > 0 ? options.BlockTypes : renderer.GetSupportedBlockTypes()?.ToArray();

        var customView = new Dictionary<string, object>
        {
            ["type"] = "blockEditorCustomView",
            ["alias"] = "FrontendBlockPreview.BlockCustomView",
            ["name"] = "Frontend Block Preview",
            ["element"] = $"{Constants.AppPluginsPath}/block-preview.js",
            ["forBlockEditor"] = new[] { "block-list", "block-grid" },
        };

        if (blockTypes is not null)
        {
            customView["forContentTypeAlias"] = blockTypes
                .Except(options.ExcludedBlockTypes, StringComparer.OrdinalIgnoreCase)
                .Order()
                .ToArray();
        }

        var manifest = new PackageManifest
        {
            Name = Constants.PackageName,
            Id = Constants.PackageName,
            Version = typeof(FrontendBlockPreviewManifestReader).Assembly.GetName().Version?.ToString(3),
            AllowTelemetry = true,
            Extensions = [customView],
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
