WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.XmlSitemapsForUmbraco.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.XmlSitemapsForUmbraco.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile("appsettings.Development.XmlSitemapsForUmbraco.json", optional: true, reloadOnChange: true)
        .AddJsonFile("appsettings.Development.HttpHeadersForUmbraco.json", optional: true, reloadOnChange: true);
}

if (builder.Environment.IsProduction())
{
    builder.Configuration
        .AddJsonFile("appsettings.Production.RobotsTxtForUmbraco.json", optional: true, reloadOnChange: true);
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

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
