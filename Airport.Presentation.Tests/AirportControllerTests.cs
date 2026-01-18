using Airport.Contracts.Providers;
using Airport.Models;
using Airport.Services;

namespace Airport.Presentation.Tests
{
    public class AirportControllerTests
    {
        #region Fields
        private AirportController _airportController;
        private readonly Mock<IAirportService> _mockAirportService;
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        #endregion

        public AirportControllerTests()
        {
            _mockAirportService = new Mock<IAirportService>();
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
            _airportController = new AirportController(_mockAirportService.Object);
        }

        [Fact]
        public void Created_NotNull() => Assert.NotNull(_airportController);

        [Fact]
        public async Task StartAsync_AirportStarted_ReturnsOkAsync()
        {
            _mockAirportService
                .SetupSequence(x => x.StartAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Started")
                .ReturnsAsync("Already started");

            var resultFirst = await _airportController.StartAsync(It.IsAny<CancellationToken>());
            var resultSecond = await _airportController.StartAsync(It.IsAny<CancellationToken>());

            var okFirstResult = Assert.IsType<OkObjectResult>(resultFirst);
            var okSecondResult = Assert.IsType<OkObjectResult>(resultSecond);

            Assert.Equivalent(okFirstResult.Value, "Started");
            Assert.Equivalent(okSecondResult.Value, "Already started");
        }

        [Fact]
        public async Task StatusAsync_WhenCalled_ReturnsAirportStatusAsync()
        {
            var stationDto = new StationDTO();
            var routeDto = new RouteDTO();
            var expected = new AirportStatus
            {
                Stations = new List<StationDTO> { stationDto },
                Routes = new List<RouteDTO> { routeDto },
            };

            _mockAirportService
                .Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            var result = await _airportController.StatusAsync(It.IsAny<CancellationToken>());

            var actual = Assert.IsType<OkObjectResult>(result);
            Assert.Equivalent(expected, actual.Value);
        }

        [Fact]
        public async Task SummaryAsync_WhenCalled_ReturnsValueAsync()
        {
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };
            
            var expected = new List<FlightSummary>
            {
                new FlightSummary
                {
                    FlightId = departure.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Departure
                },
                new FlightSummary
                {
                    FlightId = landing.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Landing
                }
            }
            .ToPagedList(1, 1);

            _airportController.ControllerContext.HttpContext = new DefaultHttpContext();
            
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            var result = await _airportController.SummaryAsync(
                new GetSummaryParameters
                {
                    PageNumber = 1,
                    PageSize = 1
                },
                It.IsAny<CancellationToken>());

            var actual = Assert.IsType<OkObjectResult>(result);
            Assert.Equivalent(expected, actual.Value);
        }

        [Fact]
        public async Task SummaryAsync_ArgumentIsNull_ReturnsValueAsync()
        {
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };

            var expected = new List<FlightSummary>
            {
                new FlightSummary
                {
                    FlightId = departure.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Departure
                },
                new FlightSummary
                {
                    FlightId = landing.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Landing
                }
            }
            .ToPagedList(1, 1);

            _airportController.ControllerContext.HttpContext = new DefaultHttpContext();

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _mockAirportService
                .Setup(x => x.GetSummaryWithMetadataAsync(
                    It.IsAny<GetSummaryParameters>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new ArgumentNullException());

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _airportController.SummaryAsync(null!, It.IsAny<CancellationToken>()));

            //var actual = Assert.IsType<OkObjectResult>(result);
            //Assert.Equivalent(expected, actual.Value);
        }
    }
}