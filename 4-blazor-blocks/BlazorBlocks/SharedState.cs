using System.Collections.Concurrent;
using Blazor.Diagrams;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;

namespace BlazorBlocks;

public static class SharedState
{
    public static IReadOnlyList<Point> Points { get; set; } = new List<Point>();
    public static ConcurrentDictionary<Guid, Point> Registry { get; } = new();

    public static void UpdatePoint(Guid id, Point point)
    {
        Registry[id] = point;
        Points = Registry.Values.ToList();
    }

    public static Action<IReadOnlyList<Point>> OnStateChanged { get; set; }

    public static BlazorDiagram Diagram { get; set; } = CreateDiagram();

    public static BlazorDiagram CreateDiagram()
    {
        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = true,
            AllowPanning = true,
            Virtualization =
            {
                Enabled = true,
            },
            Zoom =
            {
                Enabled = true,
            },
            Links =
            {
                DefaultRouter = new NormalRouter(),
                DefaultPathGenerator = new SmoothPathGenerator()
            },
        };

        var diagram = new BlazorDiagram(options);

        var firstNode = diagram.Nodes.Add(new NodeModel(position: new Point(50, 50))
        {
            Title = "Node 1",
        });

        firstNode.AddPort(PortAlignment.Left);
        var right1 = firstNode.AddPort(PortAlignment.Right);
        var secondNode = diagram.Nodes.Add(new NodeModel(position: new Point(200, 100))
        {
            Title = "Node 2",
        });
        var leftPort = secondNode.AddPort(PortAlignment.Left);
        var rightPort = secondNode.AddPort(PortAlignment.Right);

        // The connection point will be the intersection of
        // a line going from the target to the center of the source
        var sourceAnchor = new SinglePortAnchor(right1);
        // The connection point will be the port's position
        var targetAnchor = new SinglePortAnchor(leftPort);
        var link = diagram.Links.Add(new LinkModel(sourceAnchor, targetAnchor));

        return diagram;
    }
}