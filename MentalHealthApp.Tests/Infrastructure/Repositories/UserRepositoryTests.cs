using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Infrastructure.Repositories;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    private Mock<IMongoCollection<User>> _collectionMock;
    private UserRepository _userRepository;

    [SetUp]
    public void Setup()
    {
        _collectionMock = new Mock<IMongoCollection<User>>();
        _userRepository = new UserRepository(_collectionMock.Object, new Mock<IResilienceAuditLogger>().Object);
    }

    [Test]
    public async Task EmailExistsAsync_ExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var email = "existing@example.com";

        _collectionMock.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        var result = await _userRepository.EmailExistsAsync(email);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task EmailExistsAsync_NonExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _collectionMock.Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await _userRepository.EmailExistsAsync(email);

        // Assert
        Assert.That(result, Is.False);
    }
}