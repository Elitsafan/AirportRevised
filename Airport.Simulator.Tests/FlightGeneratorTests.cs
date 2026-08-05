namespace Airport.Simulator.Tests
{
    public class FlightGeneratorTests
    {
        [Fact]
        public void GenerateFlight_WhenCalled_ReturnsValue()
        {
            // Arrange
            IFlightGenerator generator = new FlightGenerator();

            // Act
            var departureForCreationDto = generator.GenerateFlight(FlightType.Departure);
            var landingForCreationDto = generator.GenerateFlight(FlightType.Landing);

            // Assert
            Assert.IsAssignableFrom<FlightForCreationDTO>(departureForCreationDto);
            Assert.IsAssignableFrom<FlightForCreationDTO>(landingForCreationDto);
            Assert.NotNull(departureForCreationDto);
            Assert.NotNull(landingForCreationDto);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void GenerateFlights_WhenCalled_ReturnsCollection(int n)
        {
            // Arrange
            IFlightGenerator generator = new FlightGenerator();

            // Act
            var flights = generator.GenerateFlights(n);

            // Assert
            Assert.True(flights.Count() == n);
            foreach (var flight in flights)
            {
                Assert.NotNull(flight);
                Assert.IsAssignableFrom<FlightForCreationDTO>(flight);
            }
        }
    }
}