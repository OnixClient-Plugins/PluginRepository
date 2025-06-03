using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Version = "v1",
        Title = "My API",
        Description = "An ASP.NET Core Web API for managing weather forecasts",
        Contact = new OpenApiContact {
            Name = "Your Name",
            Email = "your.email@example.com",
            Url = new Uri("https://your.website.com"),
        }
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme {
        Description = "API Key needed to access some of the endpoints",
        Name = "Balls-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, new string[] { } } });
});
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<ApplicationDataPaths>(new ApplicationDataPaths(Path.Combine(Directory.GetCurrentDirectory(), "data")));
builder.Services.AddSingleton<IApiKeyValidator, ApiKeyValidatorService>();
builder.Services.AddSingleton<IPluginCompilerService, PluginCompilerService>();
builder.Services.AddSingleton<IOnixRuntimesService, OnixRuntimesService>();
builder.Services.AddSingleton<IPluginPublisherService, PluginPublisherService>();
builder.Services.AddSingleton<IPluginSourceDiscoveryService, PluginSourceDiscoveryService>();
builder.Services.AddSingleton<IPublishedPluginsService, PublishedPluginsService>();
builder.Services.AddSingleton<IPluginUpdaterService, PluginUpdaterService>();
builder.Services.AddSingleton<ITrustedDeveloperService, TrustedDeveloperService>();
builder.Services.AddSingleton<ITrustedPluginSourcesService, TrustedPluginSourcesService>();
builder.Services.AddSingleton<IPluginTrustService, PluginTrustService>();
builder.Services.AddSingleton<IPluginDownloadCountTrackerService, PluginDownloadCountTrackerService>();
builder.Services.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<IPluginDownloadCountTrackerService>());
builder.Services.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<IPublishedPluginsService>());
builder.Services.AddSingleton<IHostedService>(sp => (IHostedService)sp.GetRequiredService<IPluginUpdaterService>());


var app = builder.Build();

// Plugin Creator Website
app.Use(async (context, next) => {
    if (context.Request.Path == "/") {
        context.Response.Redirect("/docs/latest/guide/getting-started.html");
        return;
    }
    if (context.Request.Path == "/PluginGenerator") {
        context.Response.Redirect("/PluginGenerator/");
        return;
    }
    if (context.Request.Path == "/PluginGenerator/") {
        context.Request.Path = "/PluginGenerator/index.html";
    }
    await next();
});
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "data/websites/plugin-generator")),
    RequestPath = "/PluginGenerator",
    RedirectToAppendTrailingSlash = true
});

app.UseRouting();
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is not null && path.StartsWith("/docs") && !Path.HasExtension(path) && !path.EndsWith("/"))
    {
        context.Response.Redirect(path + "/");
        return;
    }
    await next();
});



// Eagerly load services
var onixRumtimeService = app.Services.GetRequiredService<IOnixRuntimesService>();
var trustedPluginSourcesService = app.Services.GetRequiredService<ITrustedPluginSourcesService>();
var pluginTrustService = app.Services.GetRequiredService<IPluginTrustService>();
var pluginSourceDiscoveryService = app.Services.GetRequiredService<IPluginSourceDiscoveryService>();
var trustedDeveloperService = app.Services.GetRequiredService<ITrustedDeveloperService>();

app.Urls.Add("http://*:80");

#if DEBUG
//if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseSwagger(); 
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); c.RoutePrefix = "swagger";  });
//}
#endif

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();


