using Dotify.Server.Data;
using Dotify.Server.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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
builder.AddDotifyAuth();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DotifyDbContext>().Database.Migrate();
    await AdminSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api").RequireAuthorization();
api.MapAuthEndpoints();

app.MapDefaultEndpoints();
app.UseFileServer();

app.Run();

public partial class Program { }
