using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Infrastructure.Repositories;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Repositories;

[TestFixture]
public class GuardRailRepositoryTests
{
    private Mock<IMongoCollection<GuardRail>> _collectionMock;
    private GuardRailRepository _guardRailRepository;

    [SetUp]
    public void Setup()
    {
        _collectionMock = new Mock<IMongoCollection<GuardRail>>();
        _guardRailRepository = new GuardRailRepository(_collectionMock.Object, new Mock<IResilienceAuditLogger>().Object);
    }

    [Test]
    public async Task DeleteByPatientUserIdAsync_ShouldDeleteAllGuardRailsForPatient()
    {
        // Arrange
        var patientId = "patient123";

        _collectionMock.Setup(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<GuardRail>>(), It.IsAny<DeleteOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(3));

        // Act
        await _guardRailRepository.DeleteByPatientUserIdAsync(patientId);

        // Assert
        _collectionMock.Verify(c => c.DeleteManyAsync(It.IsAny<FilterDefinition<GuardRail>>(), It.IsAny<DeleteOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}