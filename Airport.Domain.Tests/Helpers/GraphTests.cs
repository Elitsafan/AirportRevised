using Airport.Domain.Helpers;
using Airport.Models.DTOs;

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
        public void IsCircular_WhenSelfLoopExists_ReturnsTrue()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                ObjectId.GenerateNewId(),
            };
            var graph = new Graph<ObjectId>(vs);
            graph.AddEdge(vs[0], vs[0]); // Create cycle

            // Act & Assert
            Assert.True(graph.IsCircular());
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

        [Fact]
        public void GetParsedSections_HavingOneSection_ReturnsCorrectValue1()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003"),
                new ObjectId("000000000000000000000004"),
                new ObjectId("000000000000000000000005"),
                new ObjectId("000000000000000000000006"),
                new ObjectId("000000000000000000000007"),
            };

            var graph = new Graph<ObjectId>(vs);

            graph.AddEdge(vs[0], vs[1]);
            graph.AddEdge(vs[1], vs[2]);
            graph.AddEdge(vs[2], vs[3]);
            graph.AddEdge(vs[3], vs[4]);
            graph.AddEdge(vs[4], vs[5]);
            graph.AddEdge(vs[4], vs[6]);

            // Act
            var result = graph.GetParsedSections(new ObjectId[]
            {
                vs[3],
                vs[5],
                vs[6],
            });

            // Assert
            var section = Assert.IsType<SectionDTO<ObjectId>>(result.Single());
            Assert.Contains(vs[3], section.Origin);
            Assert.Contains(vs[4], section.SectionOnly);
            Assert.Single(section.Origin);
            Assert.Single(section.SectionOnly);
            Assert.Equal(2, section.Destination.Count);
            Assert.Contains(vs[5], section.Destination);
            Assert.Contains(vs[6], section.Destination);
        }

        [Fact]
        public void GetParsedSections_HavingOneSection_ReturnsCorrectValue2()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003"),
                new ObjectId("000000000000000000000004"),
                new ObjectId("000000000000000000000005"),
                new ObjectId("000000000000000000000006"),
                new ObjectId("000000000000000000000007"),
                new ObjectId("000000000000000000000008"),
                new ObjectId("000000000000000000000009"),
            };

            var graph = new Graph<ObjectId>(vs);

            graph.AddEdge(vs[5], vs[7]);
            graph.AddEdge(vs[6], vs[7]);
            graph.AddEdge(vs[7], vs[3]);
            graph.AddEdge(vs[3], vs[8]);

            // Act
            var result = graph.GetParsedSections(new ObjectId[]
            {
                vs[3],
                vs[5],
                vs[6],
            });

            // Assert
            var section = Assert.IsType<SectionDTO<ObjectId>>(result.Single());
            Assert.Equal(2, section.Origin.Count);
            Assert.Contains(vs[5], section.Origin);
            Assert.Contains(vs[6], section.Origin);
            Assert.Contains(vs[7], section.SectionOnly);
            Assert.Contains(vs[3], section.Destination);
            Assert.Single(section.SectionOnly);
            Assert.Single(section.Destination);
        }

        [Fact]
        public void GetParsedSections_HavingTrafficLightsInTheEnd_ReturnsCorrectValues()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003"),
                new ObjectId("000000000000000000000004"),
                new ObjectId("000000000000000000000005"),
                new ObjectId("000000000000000000000006"),
                new ObjectId("000000000000000000000007"),
                new ObjectId("000000000000000000000008"),
                new ObjectId("000000000000000000000009"),
            };

            var graph = new Graph<ObjectId>(vs);

            graph.AddEdge(vs[0], vs[1]);
            graph.AddEdge(vs[0], vs[2]);
            graph.AddEdge(vs[1], vs[3]);
            graph.AddEdge(vs[2], vs[3]);
            graph.AddEdge(vs[3], vs[4]);
            graph.AddEdge(vs[4], vs[5]);
            graph.AddEdge(vs[4], vs[6]);

            // Act
            var result = graph.GetParsedSections(new ObjectId[]
            {
                vs[0],
                vs[3],
                vs[5],
                vs[6],
            });

            // Assert
            var section = Assert.IsType<SectionDTO<ObjectId>>(result.Single());

            Assert.Single(section.Origin);
            Assert.Contains(vs[0], section.Origin);
            Assert.Equal(2, section.SectionOnly.Count);
            Assert.Contains(vs[1], section.SectionOnly);
            Assert.Contains(vs[2], section.SectionOnly);
            Assert.Single(section.Destination);
            Assert.Contains(vs[3], section.Destination);
        }

        [Fact]
        public void GetParsedSections_HavingTwoSections_ReturnsCorrectValues()
        {
            // Arrange
            var vs = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003"),
                new ObjectId("000000000000000000000004"),
                new ObjectId("000000000000000000000005"),
                new ObjectId("000000000000000000000006"),
                new ObjectId("000000000000000000000007"),
                new ObjectId("000000000000000000000008"),
                new ObjectId("000000000000000000000009"),
            };

            var graph = new Graph<ObjectId>(vs);

            graph.AddEdge(vs[0], vs[1]);
            graph.AddEdge(vs[0], vs[2]);
            graph.AddEdge(vs[1], vs[3]);
            graph.AddEdge(vs[2], vs[3]);
            graph.AddEdge(vs[3], vs[4]);
            graph.AddEdge(vs[4], vs[5]);
            graph.AddEdge(vs[5], vs[6]);
            graph.AddEdge(vs[5], vs[7]);
            graph.AddEdge(vs[6], vs[8]);
            graph.AddEdge(vs[7], vs[8]);

            // Act
            var result = graph.GetParsedSections(new ObjectId[]
            {
                vs[0],
                vs[3],
                vs[5],
                vs[8],
            });

            // Assert
            var sections = Assert.IsType<List<SectionDTO<ObjectId>>>(result.ToList());

            Assert.Equal(2, sections.Count);

            Assert.Single(sections[0].Origin);
            Assert.Contains(vs[0], sections[0].Origin);
            Assert.Equal(2, sections[0].SectionOnly.Count);
            Assert.Contains(vs[1], sections[0].SectionOnly);
            Assert.Contains(vs[2], sections[0].SectionOnly);
            Assert.Single(sections[0].Destination);
            Assert.Contains(vs[3], sections[0].Destination);

            Assert.Single(sections[1].Origin);
            Assert.Contains(vs[5], sections[1].Origin);
            Assert.Equal(2, sections[1].SectionOnly.Count);
            Assert.Contains(vs[6], sections[1].SectionOnly);
            Assert.Contains(vs[7], sections[1].SectionOnly);
            Assert.Single(sections[1].Destination);
            Assert.Contains(vs[8], sections[1].Destination);
        }

        [Fact]
        public void AnyDestinationOverlaps_ReturnsCorrectResult()
        {
            // Arrange
            var vs1 = new List<ObjectId>
            {
                new ObjectId("000000000000000000000001"),
                new ObjectId("000000000000000000000002"),
                new ObjectId("000000000000000000000003"),
                new ObjectId("000000000000000000000004"),
            };
            var vs2 = new List<ObjectId>
            {
                new ObjectId("000000000000000000000005"),
                new ObjectId("000000000000000000000006"),
                new ObjectId("000000000000000000000007"),
                new ObjectId("000000000000000000000008"),
            };

            var graph1 = new Graph<ObjectId>(vs1);
            var graph2 = new Graph<ObjectId>(vs2);

            graph1.AddEdge(vs1[0], vs1[1]);
            graph1.AddEdge(vs1[0], vs1[2]);
            graph1.AddEdge(vs1[1], vs1[3]);
            graph1.AddEdge(vs1[2], vs1[3]);

            graph2.AddEdge(vs2[0], vs2[1]);
            graph2.AddEdge(vs2[0], vs2[2]);
            graph2.AddEdge(vs2[1], vs2[3]);
            graph2.AddEdge(vs2[2], vs2[3]);

            // Act
            var result1 = graph1.GetParsedSections(new ObjectId[]
            {
                vs1[0],
                vs1[3],
            });
            var result2 = graph2.GetParsedSections(new ObjectId[]
            {
                vs2[0],
                vs2[3],
            });

            // Assert
            var sections1 = Assert.IsType<List<SectionDTO<ObjectId>>>(result1.ToList());
            var sections2 = Assert.IsType<List<SectionDTO<ObjectId>>>(result2.ToList());

            Assert.True(sections1.AnyDestinationOverlaps(sections2));
        }
    }
}
