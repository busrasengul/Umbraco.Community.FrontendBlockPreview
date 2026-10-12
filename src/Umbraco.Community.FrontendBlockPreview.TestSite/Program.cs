using Umbraco.Cms.Web.Common.ApplicationBuilder;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

// The demo headless frontend (frontend/server.mjs) calls the Delivery API from the browser
builder.Services.Configure<UmbracoPipelineOptions>(options =>
    options.AddFilter(new UmbracoPipelineFilter("DemoFrontendCors")
    {
        PostPipeline = app => app.UseCors(cors => cors
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()),
    }));

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
