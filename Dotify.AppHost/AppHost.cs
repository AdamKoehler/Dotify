var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume();
var dotifyDb = sql.AddDatabase("dotify");

var server = builder.AddProject<Projects.Dotify_API>("server")
    .WithReference(dotifyDb)
    .WaitFor(dotifyDb)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../Dotify.UI")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
