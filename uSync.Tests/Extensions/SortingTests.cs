using System;
using System.Collections.Generic;

using NUnit.Framework;

using uSync.Core.Dependency;

namespace uSync.Tests.Extensions;

[TestFixture]
public class SortingTests
{
    private HashSet<int> _nodes = new HashSet<int>
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10
        };

    private List<Guid> _guidNodes = new List<Guid>
    {
        Guid.Parse("{5E37F691-FF91-45DA-9F53-8641FD9FE233}"), // 0
        Guid.Parse("{5A35701C-349C-4AAE-BBCE-964B5C196989}"), // 1
        Guid.Parse("{C70BE6CF-4923-4E2B-8742-B90FB7BBAFCB}"), // 2
        Guid.Parse("{DE77C37C-DBC7-4806-8B0F-332BC38A87A4}"), // 3
        Guid.Parse("{3C90BAD8-09F2-486E-BAA7-474F0D423C4D}")  // 4
    };

    [Test]
    public void GraphSortGuidNodes()
    {
        var graph = new List<GraphEdge<Guid>>();
        
        graph.Add(GraphEdge.Create(_guidNodes[2], _guidNodes[1]));
        graph.Add(GraphEdge.Create(_guidNodes[2], _guidNodes[4]));

        var result = _guidNodes.TopologicalSort(graph);

        var expected = new List<Guid>
        {
            Guid.Parse("{5E37F691-FF91-45DA-9F53-8641FD9FE233}"), // 0
            Guid.Parse("{5A35701C-349C-4AAE-BBCE-964B5C196989}"), // 1
            Guid.Parse("{DE77C37C-DBC7-4806-8B0F-332BC38A87A4}"), // 3
            Guid.Parse("{3C90BAD8-09F2-486E-BAA7-474F0D423C4D}"),  // 4
            Guid.Parse("{C70BE6CF-4923-4E2B-8742-B90FB7BBAFCB}"), // 2
        };

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void GraphSortNodes()
    {
        var graph = new HashSet<GraphEdge<int>>(
            new[]
            {
                GraphEdge.Create(2,4),
                GraphEdge.Create(4,7),
                GraphEdge.Create(5,8),
                GraphEdge.Create(6,9),
            });


        var result = _nodes.TopologicalSort(graph);
        var expected = new List<int> {
            1, 3, 7, 8, 9, 10, 4, 5, 6, 2
        };

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void CircularDependencyReturnsNull()
    {
        var graph = new HashSet<GraphEdge<int>>(
            new[]
            {
                GraphEdge.Create(2,4),
                GraphEdge.Create(4,7),
                GraphEdge.Create(5,8),
                GraphEdge.Create(6,9),
                GraphEdge.Create(7,2), // 7 can't depend on 2 and it depends on 4 which depends on 7
            });

        var result = _nodes.TopologicalSort(graph);

        Assert.IsNull(result);
    }
}
