using Airport.Domain.Helpers;

namespace Airport.Domain.Tests.Helpers
{
    public class GraphTests
    {
        [Fact]
        public void Created_WhenCollectionIsNull_ThrowsArgumentNullException() =>
            Assert.Throws<ArgumentNullException>(() => new Graph<ObjectId>(null!));

        [Fact]
        public void AddEdge_WhenNodeDoesNotExists_ThrowsArgumentException()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                ObjectId.GenerateNewId(),
            };
            var graph = new Graph<ObjectId>(vs);
            var unknownId = ObjectId.GenerateNewId();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => graph.AddEdge(unknownId, vs[0]));
            Assert.Equal("from", ex.ParamName);
            ex = Assert.Throws<ArgumentException>(() => graph.AddEdge(vs[0], unknownId));
            Assert.Equal("to", ex.ParamName);
        }

        [Fact]
        public void IsCircular_WhenCycleExists_ReturnsTrue()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                ObjectId.GenerateNewId(),
                ObjectId.GenerateNewId(),
                ObjectId.GenerateNewId(),
            };
            var graph = new Graph<ObjectId>(vs);
            graph.AddEdge(vs[0], vs[1]);
            graph.AddEdge(vs[1], vs[2]);
            graph.AddEdge(vs[2], vs[0]); // Create cycle

            // Act & Assert
            Assert.True(graph.IsCircular());
        }

        [Fact]
        public void IsCircular_WhenCycleNotExists_ReturnsFalse()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                ObjectId.GenerateNewId(),
                ObjectId.GenerateNewId(),
            };
            var graph = new Graph<ObjectId>(vs);
            graph.AddEdge(vs[0], vs[1]);

            // Act & Assert
            Assert.False(graph.IsCircular());
        }

        [Fact]
        public void GetEnumerator_ReturnAllNodes()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                ObjectId.GenerateNewId(),
                ObjectId.GenerateNewId(),
            };
            var graph = new Graph<ObjectId>(vs);

            // Act & Assert
            Assert.Equal(vs.Count, graph.Count());
            foreach (var node in graph)
                Assert.Contains(node, graph);
        }
    }
}
