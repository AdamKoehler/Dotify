using Dotify.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.Configure<DotifyOptions>(builder.Configuration.GetSection(DotifyOptions.SectionName));
var dotifyOptions = builder.Configuration.GetSection(DotifyOptions.SectionName).Get<DotifyOptions>() ?? new();
var dataRoot = dotifyOptions.ResolveDataRoot(builder.Environment);
Directory.CreateDirectory(dataRoot);
Directory.CreateDirectory(Path.Combine(dataRoot, "audio"));
Directory.CreateDirectory(Path.Combine(dataRoot, "covers"));
builder.AddSqlServerDbContext<DotifyDbContext>("dotify");

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DotifyDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

app.MapDefaultEndpoints();
app.UseFileServer();

app.Run();

public partial class Program { }
