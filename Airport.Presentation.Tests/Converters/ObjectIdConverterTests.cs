using Newtonsoft.Json;

namespace Airport.Presentation.Tests.Converters
{
    public class ObjectIdConverterTests
    {
        [Fact]
        public async Task ReadJson_ValidInput_ConvertToObjectId()
        {
            // Arrange
            var id = ObjectId.GenerateNewId();
            using var stringReader = new StringReader($"\"{id}\"");
            await using var jsonReader = new JsonTextReader(stringReader);
            var serializer = new JsonSerializer();
            var sut = new ObjectIdConverter();

            // Act
            await jsonReader.ReadAsStringAsync();
            var result = sut.ReadJson(jsonReader, typeof(string), id, true, serializer);

            // Assert
            Assert.Equal(id, result);
        }

        [Fact]
        public async Task ReadJson_InvalidInput_ThrowsException()
        {
            // Arrange
            var id = ObjectId.GenerateNewId();
            using var stringReader = new StringReader($"\"12{id}21\"");
            await using var jsonReader = new JsonTextReader(stringReader);
            var serializer = new JsonSerializer();
            var sut = new ObjectIdConverter();

            // Act & Assert
            await jsonReader.ReadAsStringAsync();
            var ex = Assert.Throws<ArgumentException>(() => sut.ReadJson(jsonReader, typeof(string), id, true, serializer));
            Assert.Equal("Cannot parse id.", ex.Message);
        }

        [Fact]
        public async Task WriteJson_ValidInput_ConvertObjectIdToString()
        {
            // Arrange
            var id = ObjectId.GenerateNewId();
            using var stringWriter = new StringWriter();
            await using var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();
            var sut = new ObjectIdConverter();

            // Act
            sut.WriteJson(jsonWriter, id, serializer);
            await jsonWriter.FlushAsync();

            // Assert
            Assert.Equal(id.ToString(), stringWriter.ToString().Trim('"'));
        }
    }
}
