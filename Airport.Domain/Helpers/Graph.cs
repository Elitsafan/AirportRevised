namespace Airport.Domain.Helpers
{
    public class Graph
    {
        private readonly Dictionary<ObjectId, int> _idToIndex;
        private readonly Dictionary<int, ObjectId> _indexToId;
        private List<int>[] _adj; // Adjacency list

        public Graph(IEnumerable<ObjectId> ids)
        {
            _idToIndex = new Dictionary<ObjectId, int>();
            _indexToId = new Dictionary<int, ObjectId>();
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

        // Add w to v's list.
        public void AddEdge(ObjectId from, ObjectId to)
        {
            if (!_idToIndex.TryGetValue(from, out int fromIndex))
                throw new ArgumentException($"Id {from} not found in route", nameof(from));
            if (!_idToIndex.TryGetValue(to, out int toIndex))
                throw new ArgumentException($"Id {to} not found in route", nameof(to));

            _adj[fromIndex].Add(toIndex);
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

        // Function to check if the graph contains a cycle
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
    }
}
