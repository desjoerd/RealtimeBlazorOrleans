var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorPush>("BlazorPush")
    .WithExternalHttpEndpoints();

builder.Build().Run();