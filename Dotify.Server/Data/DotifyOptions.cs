namespace Dotify.Server.Data;

public class DotifyOptions
{
    public const string SectionName = "Dotify";
    public string? DataRoot { get; set; }
    public int MaxUploadSizeMb { get; set; } = 200;
    public AdminSeedOptions Admin { get; set; } = new();

    public string ResolveDataRoot(IHostEnvironment env) =>
        DataRoot ?? Path.Combine(env.ContentRootPath, "data");
}

public class AdminSeedOptions
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
