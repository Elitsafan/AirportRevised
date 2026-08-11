using Airport.Domain.Repositories;
using Airport.Models.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.IntegrationTests
{
    public class FlightRepositoryIntegrationTests : IClassFixture<FlightRepositoryIntegrationTests.LimitTestFactory>
    {
        // We create a custom factory just for this test so we can safely override the Max limit to 5
        public class LimitTestFactory : CustomWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AirportDbConfiguration:MaxFlightDocuments"] = "5"
                    });
                });
            }
        }

        private readonly LimitTestFactory _factory;

        public FlightRepositoryIntegrationTests(LimitTestFactory factory) => _factory = factory;

        [Fact]
        public async Task EnforceStorageLimitAsync_DeletesOldestFlights_WhenLimitIsExceeded()
        {
            // Arrange
            var repoManager = _factory.Services.GetRequiredService<IRepositoryManager>();

            var flightRepo = repoManager.FlightRepository;

            // Note: Since CustomWebApplicationFactory spins up a fresh Testcontainer, 
            // the database is clean at the start of every test run.
            var baseTime = DateTime.UtcNow.AddHours(-1);

            // Insert 8 flights (Our limit is 5)
            // They are inserted with incrementally later Entrance times
            for (int i = 0; i < 8; i++)
            {
                var flight = new Flight
                {
                    FlightId = ObjectId.GenerateNewId(),
                    OccupationDetails = new List<OccupationDetails>
                    {
                        new OccupationDetails
                        {
                            StationId = ObjectId.GenerateNewId(),
                            Entrance = baseTime.AddMinutes(i)
                        }
                    }
                };

                await flightRepo.AddOneAsync(flight);

                flightRepo.AddCompletedFlight(flight);
            }

            await flightRepo.FlushAsync();

            // Act - Trigger the cleanup logic
            var deletedCount = await flightRepo.EnforceStorageLimitAsync();

            // Assert
            deletedCount.Should().Be(3); // 8 inserted - 5 max = 3 deleted

            var allRemainingFlights = await flightRepo.GetAllAsync();
            var flightsList = allRemainingFlights.ToList();

            flightsList.Should().HaveCount(5); // Only the max allowed should remain

            // Verify that the oldest 3 were deleted (minutes 0, 1, 2)
            // and only the newest 5 remain (minutes 3, 4, 5, 6, 7)
            foreach (var flight in flightsList)
                flight.OccupationDetails[0].Entrance.Should().BeOnOrAfter(baseTime.AddMinutes(3));
        }
    }
}
