using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Aliases = Umbraco.Cms.Core.Constants.PropertyEditors.Aliases;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.FrontendBlockPreview.TestSite.Seeding;

public class DemoSiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DemoSiteSeeder>();
}

/// <summary>
/// Creates a small landing page site on first run: three block types (one with a dropdown, one with a nested
/// block list) and a published Home page using them.
/// </summary>
public class DemoSiteSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string PageAlias = "landingPage";

    private readonly IRuntimeState _runtimeState;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentService _contentService;
    private readonly ITemplateService _templateService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;
    private readonly IWebHostEnvironment _environment;

    public DemoSiteSeeder(
        IRuntimeState runtimeState,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IContentService contentService,
        ITemplateService templateService,
        IShortStringHelper shortStringHelper,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
        IWebHostEnvironment environment)
    {
        _runtimeState = runtimeState;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _contentService = contentService;
        _templateService = templateService;
        _shortStringHelper = shortStringHelper;
        _propertyEditors = propertyEditors;
        _configurationEditorJsonSerializer = configurationEditorJsonSerializer;
        _environment = environment;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run || _contentTypeService.Get(PageAlias) is not null)
        {
            return;
        }

        var textstring = await BuiltInAsync(Aliases.TextBox);
        var textarea = await BuiltInAsync(Aliases.TextArea);
        var theme = await CreateDataTypeAsync("Demo - Theme", Aliases.DropDownListFlexible, "Umb.PropertyEditorUi.Dropdown", new Dictionary<string, object>
        {
            ["items"] = new[] { "Light", "Dark", "Brand" },
            ["multiple"] = false,
        });

        var card = await CreateElementTypeAsync("featureCard", "Feature Card", "icon-document", ("title", "Title", textstring), ("text", "Text", textarea));
        var cards = await CreateBlockListAsync("Demo - Feature Cards", card);

        var hero = await CreateElementTypeAsync("heroBlock", "Hero", "icon-picture",
            ("title", "Title", textstring), ("intro", "Intro", textarea), ("buttonText", "Button text", textstring), ("theme", "Theme", theme));
        var features = await CreateElementTypeAsync("featuresBlock", "Features", "icon-thumbnails-small",
            ("heading", "Heading", textstring), ("cards", "Cards", cards));
        var quote = await CreateElementTypeAsync("quoteBlock", "Quote", "icon-quote",
            ("quote", "Quote", textarea), ("author", "Author", textstring));

        var pageBlocks = await CreateBlockListAsync("Demo - Page Blocks", hero, features, quote);

        var template = await EnsureTemplateAsync();

        var page = new ContentType(_shortStringHelper, UmbracoConstants.System.Root)
        {
            Alias = PageAlias,
            Name = "Landing Page",
            Icon = "icon-home",
            AllowedAsRoot = true,
        };
        page.AddPropertyGroup("content", "Content");
        page.AddPropertyType(new PropertyType(_shortStringHelper, pageBlocks) { Alias = "blocks", Name = "Blocks" }, "content");
        page.AllowedTemplates = [template];
        page.SetDefaultTemplate(template);
        await _contentTypeService.CreateAsync(page, UmbracoConstants.Security.SuperUserKey);

        var home = _contentService.Create("Home", UmbracoConstants.System.Root, PageAlias);
        home.TemplateId = template.Id;
        home.SetValue("blocks", BlockListValue(
            (hero, new Dictionary<string, object?>
            {
                ["title"] = "Preview blocks with your real frontend",
                ["intro"] = "Editors see exactly what visitors will see, while they type, before they save.",
                ["buttonText"] = "Get started",
                ["theme"] = JsonSerializer.Serialize(new[] { "Brand" }),
            }),
            (features, new Dictionary<string, object?>
            {
                ["heading"] = "Works with any frontend",
                ["cards"] = BlockListValue(
                    (card, new Dictionary<string, object?> { ["title"] = "Headless", ["text"] = "React, Next.js, Angular, Vue, Nuxt, Svelte or plain JavaScript." }),
                    (card, new Dictionary<string, object?> { ["title"] = "Delivery API", ["text"] = "No CMS code: the preview comes through your existing content/item lookups." }),
                    (card, new Dictionary<string, object?> { ["title"] = "MVC", ["text"] = "Razor sites render each block's own partial view." })),
            }),
            (quote, new Dictionary<string, object?>
            {
                ["quote"] = "I can finally see what I'm building without opening a second tab.",
                ["author"] = "A happy editor",
            })));

        _contentService.Save(home);
        _contentService.Publish(home, ["*"]);
    }

    private async Task<IDataType> BuiltInAsync(string editorAlias)
        => (await _dataTypeService.GetAllAsync()).First(dataType => dataType.EditorAlias == editorAlias);

    private async Task<IDataType> CreateDataTypeAsync(string name, string editorAlias, string editorUiAlias, IDictionary<string, object> configuration)
    {
        var editor = _propertyEditors[editorAlias] ?? throw new InvalidOperationException($"Missing property editor {editorAlias}");

        var dataType = new DataType(editor, _configurationEditorJsonSerializer)
        {
            Name = name,
            // The backoffice UI for the editor; without it the property shows "property editor UI could not be found"
            EditorUiAlias = editorUiAlias,
            DatabaseType = ValueTypes.ToStorageType(editor.GetValueEditor().ValueType),
            ConfigurationData = configuration,
        };

        var result = await _dataTypeService.CreateAsync(dataType, UmbracoConstants.Security.SuperUserKey);
        return result.Success ? result.Result : throw new InvalidOperationException($"Could not create {name}: {result.Status}");
    }

    private Task<IDataType> CreateBlockListAsync(string name, params IContentType[] elementTypes)
        => CreateDataTypeAsync(name, Aliases.BlockList, "Umb.PropertyEditorUi.BlockList", new Dictionary<string, object>
        {
            ["blocks"] = elementTypes.Select(elementType => new Dictionary<string, object>
            {
                ["contentElementTypeKey"] = elementType.Key,
                ["label"] = elementType.Name ?? elementType.Alias,
            }).ToArray(),
        });

    private async Task<IContentType> CreateElementTypeAsync(string alias, string name, string icon, params (string Alias, string Name, IDataType DataType)[] properties)
    {
        var elementType = new ContentType(_shortStringHelper, UmbracoConstants.System.Root)
        {
            Alias = alias,
            Name = name,
            Icon = icon,
            IsElement = true,
        };
        elementType.AddPropertyGroup("content", "Content");

        foreach (var property in properties)
        {
            elementType.AddPropertyType(new PropertyType(_shortStringHelper, property.DataType) { Alias = property.Alias, Name = property.Name }, "content");
        }

        await _contentTypeService.CreateAsync(elementType, UmbracoConstants.Security.SuperUserKey);
        return elementType;
    }

    private async Task<ITemplate> EnsureTemplateAsync()
    {
        var existing = await _templateService.GetAsync(PageAlias);
        if (existing is not null)
        {
            return existing;
        }

        var viewContent = await System.IO.File.ReadAllTextAsync(Path.Combine(_environment.ContentRootPath, "Views", "landingPage.cshtml"));
        var result = await _templateService.CreateAsync("Landing Page", PageAlias, viewContent, UmbracoConstants.Security.SuperUserKey);

        return result.Success ? result.Result : throw new InvalidOperationException($"Could not create the template: {result.Status}");
    }

    /// <summary>
    /// A Block List value in stored format.
    /// </summary>
    private static string BlockListValue(params (IContentType ElementType, Dictionary<string, object?> Values)[] blocks)
    {
        var items = blocks.Select(block => (Key: Guid.NewGuid(), block.ElementType, block.Values)).ToList();

        return JsonSerializer.Serialize(new
        {
            layout = new Dictionary<string, object>
            {
                [Aliases.BlockList] = items.Select(item => new { contentKey = item.Key }).ToArray(),
            },
            contentData = items.Select(item => new
            {
                contentTypeKey = item.ElementType.Key,
                key = item.Key,
                values = item.Values.Select(value => new
                {
                    editorAlias = item.ElementType.PropertyTypes.First(p => p.Alias == value.Key).PropertyEditorAlias,
                    culture = (string?)null,
                    segment = (string?)null,
                    alias = value.Key,
                    value = value.Value,
                }).ToArray(),
            }).ToArray(),
            settingsData = Array.Empty<object>(),
            expose = items.Select(item => new { contentKey = item.Key, culture = (string?)null, segment = (string?)null }).ToArray(),
        });
    }
}
