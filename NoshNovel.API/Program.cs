using NoshNovel.API.Middlewares;
using NoshNovel.API.Notifications;
using NoshNovel.API.Notifications.FileWatcherService;
using NoshNovel.Plugin.Contexts.NovelCrawler;
using NoshNovel.Plugin.Contexts.NovelDownloader;
using QuestPDF.Infrastructure;
using Serilog;
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// May log only to console or file or both
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/NoshNovel_Log.txt", rollingInterval: RollingInterval.Month)
    .MinimumLevel.Information()
    .CreateLogger();

// Clear out any providers that we have injected till now
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder => {
        builder.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed((host) => true)
            .AllowCredentials();
    });
});

// Add plugin service
builder.Services.AddTransient<INovelCrawlerContext, NovelCrawlerContext>();
builder.Services.AddTransient<INovelDownloaderContext, NovelDownloaderContext>();
builder.Services.AddSingleton<IPluginWatcher, PluginWatcher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add cors policy
app.UseCors("AllowAllOrigins");

// Add middleware for global error catching
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapHub<ServiceUpdateHub>("/service-update");

// Start watching on plugin service update
app.Services.GetService<IPluginWatcher>()?.StartWatcher();

app.MapControllers();

app.Run();
