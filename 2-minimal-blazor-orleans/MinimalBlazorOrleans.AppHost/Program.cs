var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("azure-storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

var orleans = builder.AddOrleans("Orleans")
    .WithClustering(tables)
    .WithGrainStorage("Default", blobs)
    .WithMemoryStreaming("DefaultStreaming")
    .WithGrainStorage("PubSubStore", tables);

var app = builder.AddProject<Projects.MinimalBlazorOrleans>("MinimalBlazorOrleans")
    .WaitFor(storage)
    .WithReference(orleans)
    .WithReference(blobs)
    .WithReference(tables)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReplicas(3);

builder.Build().Run();