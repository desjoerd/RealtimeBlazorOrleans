var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorPush>("BlazorPush")
    .WithExternalHttpEndpoints()
    .WithReplicas(3);

builder.Build().Run();