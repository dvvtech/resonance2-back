using Microsoft.Extensions.FileProviders;
using Resonance.Api.Hubs;
using Resonance.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "resonance2-front"));

if (Directory.Exists(frontendPath))
{
    var frontendFiles = new PhysicalFileProvider(frontendPath);

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFiles,
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFiles,
    });

    app.MapFallbackToFile("{*path:nonfile}", "index.html", new StaticFileOptions
    {
        FileProvider = frontendFiles,
    });
}

app.MapHub<MusicHub>("/musicHub");

app.Run();
