using Airport.Models;
using Airport.Services;

namespace Airport.Presentation.Tests
{
    public class AirportControllerTests
    {
        private AirportController _airportController;
        private Mock<IAirportService> _mockAirportService;

        public AirportControllerTests()
        {
            _mockAirportService = new Mock<IAirportService>();
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

            var actualFirst = await _airportController.StartAsync(It.IsAny<CancellationToken>());
            var actualSecond = await _airportController.StartAsync(It.IsAny<CancellationToken>());

            Assert.Equivalent(actualFirst, new OkObjectResult("Started"));
            Assert.Equivalent(actualSecond, new OkObjectResult("Already started"));
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
            _mockAirportService
                .SetupGet(x => x.HasStarted)
                .Returns(true);

            var actual = await _airportController.StatusAsync(It.IsAny<CancellationToken>());

            Assert.Equivalent(expected, (actual as dynamic).Value);
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

            var x = _airportController.ControllerContext.HttpContext = new DefaultHttpContext();
            
            _mockAirportService
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _mockAirportService
                .Setup(x => x.GetPagedSummaryAsync(
                    It.IsAny<GetSummaryParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var actual = await _airportController.SummaryAsync(
                new GetSummaryParameters
                {
                    PageNumber = 1,
                    PageSize = 1
                },
                It.IsAny<CancellationToken>());

            Assert.Equivalent(expected, (actual as dynamic).Value);
        }
    }
}