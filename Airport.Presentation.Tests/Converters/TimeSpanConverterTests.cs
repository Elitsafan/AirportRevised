using Newtonsoft.Json;

namespace Airport.Presentation.Tests.Converters
{
    public class TimeSpanConverterTests
    {
        [Fact]
        public async Task ReadJson_ValidInput_ConvertToObjectId()
        {
            // Arrange
            var time = TimeSpan.FromSeconds(123);
            using var stringReader = new StringReader($"\"{time}\"");
            await using var jsonReader = new JsonTextReader(stringReader);
            var serializer = new JsonSerializer();
            var sut = new TimeSpanConverter();

            // Act
            await jsonReader.ReadAsStringAsync();
            var result = sut.ReadJson(jsonReader, typeof(string), time, true, serializer);

            // Assert
            Assert.Equal(time, result);
        }

        [Fact]
        public async Task ReadJson_InvalidInput_ThrowsException()
        {
            // Arrange
            var time = TimeSpan.FromSeconds(123);
            using var stringReader = new StringReader("\"12%#$21\"");
            await using var jsonReader = new JsonTextReader(stringReader);
            var serializer = new JsonSerializer();
            var sut = new TimeSpanConverter();

            // Act & Assert
            await jsonReader.ReadAsStringAsync();
            var ex = Assert.Throws<ArgumentException>(() => sut.ReadJson(jsonReader, typeof(string), time, true, serializer));
            Assert.Equal("Cannot parse time.", ex.Message);
        }

        [Fact]
        public async Task WriteJson_ValidInput_ConvertTimeSpanToString()
        {
            // Arrange
            var time = TimeSpan.FromSeconds(123);
            using var stringWriter = new StringWriter();
            await using var jsonWriter = new JsonTextWriter(stringWriter);
            var serializer = new JsonSerializer();
            var sut = new TimeSpanConverter();

            // Act
            sut.WriteJson(jsonWriter, time, serializer);
            await jsonWriter.FlushAsync();

            // Assert
            Assert.Equal(time.ToString(), stringWriter.ToString().Trim('"'));
        }
    }
}
