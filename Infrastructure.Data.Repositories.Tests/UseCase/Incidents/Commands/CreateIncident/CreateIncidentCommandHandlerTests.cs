using espasyo.Application.Incidents.Commands.CreateIncident;
using espasyo.Application.Interfaces;
using espasyo.Domain.Enums;
using espasyo.Infrastructure.Data;
using espasyo.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Infrastructure.Tests.UseCase.Incidents.Commands.CreateIncident
{
    public class CreateIncidentCommandHandlerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IGeocodeService> _geocodeServiceMock;
        private readonly Mock<ILogger<CreateIncidentCommandHandler>> _loggerMock;
        private readonly CreateIncidentCommandHandler _handler;
        private readonly IncidentRepository _incidentRepository;

        public CreateIncidentCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);
            _incidentRepository = new IncidentRepository(_context);
            _geocodeServiceMock = new Mock<IGeocodeService>();
            _loggerMock = new Mock<ILogger<CreateIncidentCommandHandler>>();

            _handler = new CreateIncidentCommandHandler(
                _incidentRepository,
                _geocodeServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Theory]
        [InlineData("CASE123", "123 Main St", SeverityEnum.High, CrimeTypeEnum.Homicide, MotiveEnum.Unknown, Barangay.Alabang, WeatherConditionEnum.Fog, "2023-10-01T00:00:00Z")]
        public async Task Handle_ShouldCreateIncidentSuccessfully(string caseId, string address, SeverityEnum severity, CrimeTypeEnum crimeType, MotiveEnum motive, Barangay barangay, WeatherConditionEnum weather, DateTimeOffset timeStamp)
        {
            // Arrange
            var command = new CreateIncidentCommand
            {
                CaseId = caseId,
                Address =address,
                Severity = (int)severity,
                CrimeType = (int)crimeType,
                Motive = (int)motive,
                Precinct = (int)barangay,
                AdditionalInfo = "Details about the case",
                Weather = (int)weather,
                TimeStamp = timeStamp
            };

            var latLong = (10.0, 20.0, "Updated Address");

            _geocodeServiceMock.Setup(x => x.GetLatLongAsync(command.Address!))
                .ReturnsAsync(latLong);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            var created = await _context.Incidents.FindAsync(result);

            // Assert
            result.ShouldNotBe(Guid.Empty);
            created.ShouldNotBeNull();
            created.Address.ShouldBe(command.Address);
            created.CaseId.ShouldBe(command.CaseId);
            created.Severity.ShouldBe((SeverityEnum)command.Severity);
            created.CrimeType.ShouldBe((CrimeTypeEnum)command.CrimeType);
            created.Motive.ShouldBe((MotiveEnum)command.Motive);
            created.PoliceDistrict.ShouldBe((Barangay)command.Precinct);
            created.AdditionalInformation.ShouldBe(command.AdditionalInfo);
            created.Weather.ShouldBe((WeatherConditionEnum)command.Weather);
            created.TimeStamp.ShouldBe(command.TimeStamp);
            created.GetLatitude().ShouldBe(latLong.Item1);
            created.GetLongitude().ShouldBe(latLong.Item2);
            created.SanitizedAddress.ShouldBe(latLong.Item3);
            created.GetTimeStampInUnix.ShouldBe(command.TimeStamp!.Value.ToUnixTimeMilliseconds());

            _geocodeServiceMock.Verify(x => x.GetLatLongAsync(command.Address!), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenIncidentCreationFails()
        {
            // Arrange
            var command = new CreateIncidentCommand
            {
                CaseId = "CASE123",
                Address = "123 Main St",
                Severity = (int)SeverityEnum.High,
                CrimeType = (int)CrimeTypeEnum.Robbery,
                Motive = (int)MotiveEnum.Anger,
                Precinct = (int)Barangay.Alabang,
                AdditionalInfo = "Details about the case",
                Weather = (int)WeatherConditionEnum.Clear,
                TimeStamp = DateTimeOffset.UtcNow
            };

            var latLong = (10.0, 20.0, "Updated Address");

            _geocodeServiceMock.Setup(x => x.GetLatLongAsync(command.Address!))
                .ReturnsAsync(latLong);

            // Simulate failure by disposing of the context
            _context.Dispose();

            // Act & Assert
            await Should.ThrowAsync<ObjectDisposedException>(async () =>
            {
                await _handler.Handle(command, CancellationToken.None);
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}