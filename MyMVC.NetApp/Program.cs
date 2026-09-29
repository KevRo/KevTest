using System.Globalization;
using Anthropic;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyMVC.NetApp.Data;
using MyMVC.NetApp.Models.Claude;
using MyMVC.NetApp.Models.Strava;
using MyMVC.NetApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
{
    var baseUrl = builder.Configuration["ProductsApi:BaseUrl"] ?? "http://localhost:5132";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.Configure<StravaOptions>(builder.Configuration.GetSection(StravaOptions.SectionName));
builder.Services.AddHttpClient<IStravaApiClient, StravaApiClient>(client =>
{
    client.BaseAddress = new Uri("https://www.strava.com/");
});
builder.Services.AddDbContext<StravaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Strava") ?? "Data Source=strava.db";
    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<IStravaSyncService, StravaSyncService>();

builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection(ClaudeOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var claudeOptions = sp.GetRequiredService<IOptions<ClaudeOptions>>().Value;
    return new AnthropicClient { ApiKey = claudeOptions.ApiKey };
});
builder.Services.AddScoped<ISqlQueryTool, SqlQueryTool>();
builder.Services.AddScoped<IAskStravaService, AskStravaService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.Name = ".MyMVC.AskStrava.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var supportedCultures = new[] { "en", "ga", "it" }.Select(c => new CultureInfo(c)).ToArray();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var stravaDb = scope.ServiceProvider.GetRequiredService<StravaDbContext>();
    stravaDb.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
