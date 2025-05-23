var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("azure-storage")
    .RunAsEmulator(x => x
        .WithBlobPort(10000)
        .WithQueuePort(10001)
        .WithTablePort(10002)
        .WithDataVolume()
    );

var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");
var tables = storage.AddTables("tables");

var orleans = builder.AddOrleans("Orleans")
    .WithClustering(tables)
    .WithClusterId(Guid.NewGuid().ToString())
    .WithGrainStorage("Default", blobs)
    .WithGrainStorage("PubSubStore", tables)
    .WithMemoryStreaming("DefaultStreaming");

var app = builder.AddProject<Projects.ParkAid_WebApp>("WebApp")
    .WaitFor(storage)
    .WithReference(orleans)
    .WithReplicas(3)
    .WithExternalHttpEndpoints();

builder.Build().Run();