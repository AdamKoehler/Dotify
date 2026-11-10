var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume();
var dotifyDb = sql.AddDatabase("dotify");

var adminPassword = builder.AddParameter("admin-password", secret: true);

const int viteDevPort = 5173;

var publicBaseUrl = $"http://localhost:{viteDevPort}";

var server = builder.AddProject<Projects.Dotify_API>("server")
    .WithReference(dotifyDb)
    .WaitFor(dotifyDb)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", ep => new() { Url = "/scalar", DisplayText = "Scalar" })
    .WithExternalHttpEndpoints()
    .WithEnvironment("Dotify__Admin__UserName", "admin")
    .WithEnvironment("Dotify__Admin__Password", adminPassword);

if (builder.ExecutionContext.IsRunMode)
{
    server.WithEnvironment("Dotify__PublicBaseUrl", publicBaseUrl);
}

var webfrontend = builder.AddViteApp("webfrontend", "../Dotify.UI")
    .WithHttpEndpoint(port: viteDevPort, env: "PORT", isProxied: false)
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
