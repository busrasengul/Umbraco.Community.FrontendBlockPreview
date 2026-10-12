using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Community.FrontendBlockPreview.Configuration;
using Umbraco.Community.FrontendBlockPreview.Rendering;
using Umbraco.Community.FrontendBlockPreview.Rendering.DeliveryApi;
using Umbraco.Community.FrontendBlockPreview.Rendering.Razor;
using Umbraco.Community.FrontendBlockPreview.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.FrontendBlockPreview.Composing;

/// <summary>
/// Registers the package. The renderer comes from FrontendBlockPreview:Renderer; with "Custom" the project
/// registers its own <see cref="IBlockPreviewRenderer"/> (and optionally <see cref="IFrontendUrlResolver"/>).
/// </summary>
public class FrontendBlockPreviewComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var section = builder.Config.GetSection(Constants.ConfigurationSection);
        builder.Services.Configure<FrontendBlockPreviewOptions>(section);

        builder.Services.TryAddSingleton<IBlockPreviewTokenStore, RuntimeCacheBlockPreviewTokenStore>();
        builder.Services.TryAddSingleton<IFrontendUrlResolver, ConfigurationFrontendUrlResolver>();
        builder.Services.TryAddScoped<IBlockPreviewService, BlockPreviewService>();
        // Inserted first, not appended: PackagingService resolves a single IPackageManifestReader (the last one),
        // which must stay Umbraco's App_Plugins reader. The manifest service still reads all of them.
        builder.Services.Insert(0, ServiceDescriptor.Singleton<IPackageManifestReader, FrontendBlockPreviewManifestReader>());

        var renderer = section[nameof(FrontendBlockPreviewOptions.Renderer)];

        if (string.Equals(renderer, Constants.Renderers.DeliveryApi, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.TryAddScoped<IBlockPreviewRenderer, DeliveryApiBlockPreviewRenderer>();

            // Needs AddDeliveryApi() to have run first, which it does in the default Program.cs (before AddComposers)
            builder.Services.AddUnique<IApiContentPathResolver>(serviceProvider => new BlockPreviewApiContentPathResolver(
                ActivatorUtilities.CreateInstance<ApiContentPathResolver>(serviceProvider),
                serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                serviceProvider.GetRequiredService<IPublishedValueFallback>()));
        }
        else if (string.Equals(renderer, Constants.Renderers.Razor, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.TryAddScoped<IBlockPreviewRenderer, RazorBlockPreviewRenderer>();
        }
    }
}
