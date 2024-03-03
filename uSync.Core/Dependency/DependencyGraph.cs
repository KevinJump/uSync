namespace uSync.Core.Dependency;

/// <summary>
///  builds a graph of dependencies, so things can be installed in order. 
/// </summary>
public static class DependencyGraph
{
    public static List<T>? TopologicalSort<T>(this ICollection<T> nodes, ICollection<GraphEdge<T>> edges)
        where T : IEquatable<T>
    {
        var sortedList = new List<T>();

        // all items where they don't have a dependency on themselves. 
        var queue = new Queue<T>(
            nodes.Where(x => edges.All(e => e.Node.Equals(x) == false)));


        while (queue.Any())
        {
            // remove this item add it to the queue.
            var next = queue.Dequeue();
            sortedList.Add(next);

            // look for any edges for this queue. 
            foreach (var edge in edges.Where(e => e.Edge.Equals(next)).ToList())
            {
                var dependency = edge.Node;
                edges.Remove(edge);

                if (edges.All(x => x.Node.Equals(dependency) == false))
                {
                    queue.Enqueue(dependency);
                }
            }
        }

        return edges.Count > 0 ? sortedList : null;
    }
}

public class GraphEdge<T>
    where T : IEquatable<T>
{
    public GraphEdge(T node, T edge)
    {
        Node = node;
        Edge = edge;
    } 

    public T Node { get; set; }
    public T Edge { get; set; }

}

public class GraphEdge
{
    public static GraphEdge<T> Create<T>(T node, T edge) where T : IEquatable<T>
        => new GraphEdge<T>(node,edge);
}

