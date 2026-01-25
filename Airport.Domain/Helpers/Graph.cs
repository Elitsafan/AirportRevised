using System.Collections;

namespace Airport.Domain.Helpers
{
    public sealed class Graph<T> : IEnumerable<T>
        where T : notnull, IComparable<T>, IEquatable<T>
    {
        #region Fields
        private readonly Dictionary<T, int> _idToIndex;
        private readonly Dictionary<int, T> _indexToId;
        private List<int>[] _adj; // Adjacency list 
        #endregion

        public Graph(IEnumerable<T> ids)
        {
            if (ids is null)
                throw new ArgumentNullException(nameof(ids));

            _idToIndex = new Dictionary<T, int>();
            _indexToId = new Dictionary<int, T>();

            foreach (var id in ids)
            {
                if (!_idToIndex.ContainsKey(id))
                {
                    var index = _idToIndex.Count;
                    _idToIndex[id] = index;
                    _indexToId[index] = id;
                }
            }
            _adj = new List<int>[_idToIndex.Count];
            for (var i = 0; i < _adj.Length; i++)
                _adj[i] = new List<int>();
        }

        /// <summary>
        /// Add edge to the <see cref="Graph"/> instance.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AddEdge(T from, T to)
        {
            if (!_idToIndex.TryGetValue(from, out int fromIndex))
                throw new ArgumentException($"Id {from} not found.", nameof(from));
            if (!_idToIndex.TryGetValue(to, out int toIndex))
                throw new ArgumentException($"Id {to} not found.", nameof(to));

            _adj[fromIndex].Add(toIndex);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator()
        {
            // This performs a BFS traversal starting from nodes with no incoming edges 
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            
            // Start with nodes that have no parents (roots)
            var hasInbound = new bool[_adj.Length];
            foreach (var neighbors in _adj)
                foreach (var neighbor in neighbors)
                    hasInbound[neighbor] = true; 
            
            for (int i = 0; i < hasInbound.Length; i++)
                if (!hasInbound[i]) 
                    queue.Enqueue(i);

            // If everything is a cycle, just start at 0
            if (queue.Count == 0 && _adj.Length > 0) 
                queue.Enqueue(0);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) 
                    continue;

                yield return _indexToId[current];

                foreach (var neighbor in _adj[current])
                    queue.Enqueue(neighbor);
            }
        }

        /// <summary>
        /// Check if the <see cref="Graph"/> instance contains a cycle
        /// </summary>
        /// <returns></returns>        
        public bool IsCircular()
        {
            bool[] visited = new bool[_adj.Length];
            bool[] recStack = new bool[_adj.Length];

            // Call the recursive helper function for all vertices
            // to handle disconnected components
            for (int i = 0; i < _adj.Length; i++)
                if (!visited[i])
                    if (IsCyclicUtil(i, visited, recStack))
                        return true;

            return false;
        }

        // Recursive function to perform DFS and detect cycles
        private bool IsCyclicUtil(int i, bool[] visited, bool[] recStack)
        {
            if (recStack[i])
                return true; // Cycle detected: node is in current recursion stack

            if (visited[i])
                return false; // Node was visited in another path, no cycle here

            visited[i] = true;
            recStack[i] = true;

            foreach (var neighbor in _adj[i])
                if (IsCyclicUtil(neighbor, visited, recStack))
                    return true;

            recStack[i] = false; // Backtrack: remove node from recursion stack
            return false;
        }
    }
}
