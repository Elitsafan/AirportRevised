using Airport.Contracts.Providers;
using Airport.Models;
using Airport.Services.Extensions;

namespace Airport.Presentation.Tests.Controllers
{
    public class AirportControllerTests
    {
        #region Fields
        private AirportController _airportController = null!;
        private readonly Mock<IAirportService> _mockAirportService;
        private readonly Mock<IAirportStateProvider> _mockAirportStateProvider;
        #endregion

        public AirportControllerTests()
        {
            _mockAirportStateProvider = new Mock<IAirportStateProvider>();
            _mockAirportService = new Mock<IAirportService>();
        }

        [Fact]
        public async Task StartAsync_AirportStarted_ReturnsOk()
        {
            // Arrange
            _mockAirportService
                .SetupSequence(x => x.StartAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Started")
                .ReturnsAsync("Already started");

            // Act
            _airportController = new AirportController(_mockAirportService.Object);
            var resultFirst = await _airportController.StartAsync();
            var resultSecond = await _airportController.StartAsync();

            // Assert
            var okFirstResult = Assert.IsType<OkObjectResult>(resultFirst);
            var okSecondResult = Assert.IsType<OkObjectResult>(resultSecond);
            Assert.Equal(okFirstResult.Value, "Started");
            Assert.Equal(okSecondResult.Value, "Already started");
        }

        [Fact]
        public async Task StatusAsync_WhenCalled_ReturnsAirportStatus()
        {
            // Arrange
            var stationDto = new StationDTO();
            var routeDto = new RouteDTO();
            IAirportStatus expected = new AirportStatus
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

            // Act
            _airportController = new AirportController(_mockAirportService.Object);
            var result = await _airportController.StatusAsync();

            // Assert
            var actual = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, actual.Value);
        }

        [Fact]
        public async Task SummaryAsync_WhenCalled_ReturnsValue()
        {
            // Arrange
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };
            var summary = new SummaryWithMetadata
            {
                Summary = new List<FlightSummary>
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
                .ToPagedList(1, 1),
                LandingsCount = 1,
                DeparturesCount = 1,
            };

            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _mockAirportService
                .Setup(x => x.GetSummaryWithMetadataAsync(
                    It.IsAny<GetSummaryParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(summary);

            // Act
            _airportController = new AirportController(_mockAirportService.Object);
            _airportController.ControllerContext.HttpContext = new DefaultHttpContext();
            var result = await _airportController.SummaryAsync(
                new GetSummaryParameters
                {
                    PageNumber = 1,
                    PageSize = 1
                });

            // Assert
            var actual = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(summary.Summary, actual.Value);
        }

        [Fact]
        public async Task SummaryAsync_WhenCalled_AddsPaginationHeader()
        {
            // Arrange
            var departure = new Departure { FlightId = ObjectId.GenerateNewId() };
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };
            var summary = new SummaryWithMetadata
            {
                Summary = new List<FlightSummary>
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
                .ToPagedList(1, 1),
                LandingsCount = 1,
                DeparturesCount = 1,
            };
            _mockAirportService
                .Setup(x => x.GetSummaryWithMetadataAsync(
                    It.IsAny<GetSummaryParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(summary);
            _mockAirportStateProvider
                .SetupGet(x => x.HasStarted)
                .Returns(true);
            _airportController = new AirportController(_mockAirportService.Object);
            _airportController.ControllerContext.HttpContext = new DefaultHttpContext();

            // Act
            var result = await _airportController.SummaryAsync(
                new GetSummaryParameters
                {
                    PageNumber = 1,
                    PageSize = 1
                });

            // Assert
            var response = _airportController.Response;
            Assert.True(response.Headers.ContainsKey("X-Pagination"));

            // You can even verify the content of the header
            var headerValue = response.Headers["X-Pagination"].ToString();
            Assert.Contains("{\"totalCount\":2", headerValue);
        }

        [Fact]
        public async Task SummaryAsync_WhenCalled_ReturnsValueAndSetsHeaders()
        {
            // Arrange
            var landing = new Landing { FlightId = ObjectId.GenerateNewId() };
            var flightSummaryList = new List<FlightSummary>
            {
                new FlightSummary
                {
                    FlightId = landing.FlightId,
                    Stations = new List<OccupationDetails>(),
                    FlightType = FlightType.Landing
                }
            };
            var pagedList = new PagedList<FlightSummary>(flightSummaryList, 1, 1, 1);

            var expectedSummary = new SummaryWithMetadata
            {
                Summary = pagedList, // This prevents the 'null' in your extension
                LandingsCount = 1,
                DeparturesCount = 0
            };

            _mockAirportService
                .Setup(x => x.GetSummaryWithMetadataAsync(
                    It.IsAny<GetSummaryParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSummary);

            _airportController = new AirportController(_mockAirportService.Object);
            // Mandatory for the extension method to have a 'Response' object to work with
            _airportController.ControllerContext.HttpContext = new DefaultHttpContext();

            // Act
            var result = await _airportController.SummaryAsync(
                new GetSummaryParameters
                {
                    PageNumber = 1,
                    PageSize = 1
                });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(pagedList, okResult.Value);
            Assert.True(_airportController.Response.Headers.ContainsKey("X-Pagination"));
        }
    }
}