var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("azure-storage")
    .RunAsEmulator(e => e.WithLifetime(ContainerLifetime.Persistent));

var blobs = storage.AddBlobs("blobs");
var tables = storage.AddTables("tables");

var orleans = builder.AddOrleans("Orleans")
    .WithDevelopmentClustering();
    // .WithClustering(tables)
    // This is the storage used for streaming and must be named PubSubStore
    // .WithGrainStorage("PubSubStore", tables)
    // .WithMemoryStreaming("Default");

builder.AddProject<Projects.MinimalBlazorOrleans>("MinimalBlazorOrleans")
    .WithReference(orleans)
    // .WithReference(blobs)
    // .WithReference(tables)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReplicas(1);

builder.Build().Run();