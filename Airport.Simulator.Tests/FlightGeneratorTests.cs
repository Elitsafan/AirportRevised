namespace OnionArchitecture.Simulator.Tests
{
    public class FlightGeneratorTests
    {


        [Fact]
        public void Created_NotNull() => Assert.NotNull(new FlightGenerator());

        [Fact]
        public void GenerateFlight_WhenCalled_ReturnsValue()
        {
            IFlightGenerator generator = new FlightGenerator();
            var departureForCreationDto = generator.GenerateFlight(FlightType.Departure);
            var landingForCreationDto = generator.GenerateFlight(FlightType.Landing);

            Assert.NotNull(departureForCreationDto);
            Assert.NotNull(landingForCreationDto);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void GenerateFlights_WhenCalled_ReturnsCollection(int n)
        {
            IFlightGenerator generator = new FlightGenerator();
            var flightsForCreation = generator.GenerateFlights(n);

            Assert.True(flightsForCreation.Count() == n);
        }
    }
}