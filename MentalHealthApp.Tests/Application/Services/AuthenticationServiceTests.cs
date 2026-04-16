using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Application.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IPatientProfileRepository> _patientProfileRepositoryMock;
    private Mock<ITherapistInvitationRepository> _invitationRepositoryMock;
    private Mock<ITherapistAccessRepository> _therapistAccessRepositoryMock;
    private Mock<IPasswordHashingService> _passwordHashingServiceMock;
    private Mock<IJwtTokenService> _jwtTokenServiceMock;
    private AuthenticationService _authenticationService;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _patientProfileRepositoryMock = new Mock<IPatientProfileRepository>();
        _invitationRepositoryMock = new Mock<ITherapistInvitationRepository>();
        _therapistAccessRepositoryMock = new Mock<ITherapistAccessRepository>();
        _passwordHashingServiceMock = new Mock<IPasswordHashingService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _authenticationService = new AuthenticationService(
            _userRepositoryMock.Object,
            _patientProfileRepositoryMock.Object,
            _invitationRepositoryMock.Object,
            _therapistAccessRepositoryMock.Object,
            _passwordHashingServiceMock.Object,
            _jwtTokenServiceMock.Object);
    }

    [Test]
    public async Task RegisterPatientAsync_ValidRequest_ShouldCreateUserAndProfile()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "password123",
            ConfirmPassword = "password123",
            PhoneNumber = "+1234567890"
        };

        var createdUser = new User
        {
            Id = "user123",
            Email = request.Email,
            Username = request.Username,
            PasswordHash = "hashedpassword",
            Role = UserRole.Patient,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, default))
            .ReturnsAsync((User?)null);
        _passwordHashingServiceMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashedpassword");
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(createdUser);
        _patientProfileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PatientProfile>(), default))
            .ReturnsAsync(new PatientProfile());

        // Act
        var result = await _authenticationService.RegisterPatientAsync(request);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(createdUser.Id));
        Assert.That(result.Email, Is.EqualTo(createdUser.Email));
        Assert.That(result.Username, Is.EqualTo(createdUser.Username));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
        _patientProfileRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PatientProfile>(), default), Times.Once);
    }

    [Test]
    public void RegisterPatientAsync_EmailAlreadyExists_ShouldThrowException()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            Email = "existing@example.com",
            Username = "testuser",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var existingUser = new User { Id = "existing", Email = request.Email };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, default))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.RegisterPatientAsync(request));
        Assert.That(exception.Message, Is.EqualTo("Email already registered"));
    }

    [Test]
    public async Task LoginPatientAsync_ValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new PatientLoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "password123"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.EmailOrUsername,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            Role = UserRole.Patient,
            IsActive = true
        };

        var expectedToken = "jwt-token";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername, default))
            .ReturnsAsync(user);
        _passwordHashingServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user.Id, user.Role.ToString(), user.Email))
            .Returns(expectedToken);

        // Act
        var result = await _authenticationService.LoginPatientAsync(request);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.Email, Is.EqualTo(user.Email));
        Assert.That(result.Username, Is.EqualTo(user.Username));
        Assert.That(result.Token, Is.EqualTo(expectedToken));
    }

    [Test]
    public void LoginPatientAsync_InvalidCredentials_ShouldThrowException()
    {
        // Arrange
        var request = new PatientLoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "wrongpassword"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.EmailOrUsername,
            PasswordHash = "hashedpassword",
            Role = UserRole.Patient,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername, default))
            .ReturnsAsync(user);
        _passwordHashingServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.LoginPatientAsync(request));
        Assert.That(exception.Message, Is.EqualTo("Invalid email/username or password"));
    }

    [Test]
    public void LoginPatientAsync_InactiveUser_ShouldThrowException()
    {
        // Arrange
        var request = new PatientLoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "password123"
        };

        var user = new User
        {
            Id = "user123",
            Email = request.EmailOrUsername,
            PasswordHash = "hashedpassword",
            Role = UserRole.Patient,
            IsActive = false
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername, default))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.LoginPatientAsync(request));
        Assert.That(exception.Message, Is.EqualTo("Invalid email/username or password"));
    }

    [Test]
    public async Task SetupTherapistAsync_ValidRequest_ShouldCreateTherapistAndAccess()
    {
        // Arrange
        var request = new TherapistSetupRequest
        {
            Token = "invitation-token",
            Username = "therapistuser",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var invitation = new TherapistInvitation
        {
            Id = "invitation123",
            Token = request.Token,
            TherapistEmail = "therapist@example.com",
            PatientUserId = "patient123",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var createdTherapist = new User
        {
            Id = "therapist123",
            Email = invitation.TherapistEmail,
            Username = request.Username,
            PasswordHash = "hashedpassword",
            Role = UserRole.Therapist,
            IsActive = true
        };

        var expectedToken = "jwt-token";

        _invitationRepositoryMock.Setup(x => x.GetByTokenAsync(request.Token, default))
            .ReturnsAsync(invitation);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.Username, default))
            .ReturnsAsync((User?)null);
        _passwordHashingServiceMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashedpassword");
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(createdTherapist);
        _invitationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<TherapistInvitation>(), default))
            .Returns(Task.CompletedTask);
        _therapistAccessRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TherapistAccess>(), default))
            .ReturnsAsync(new TherapistAccess());
        _patientProfileRepositoryMock.Setup(x => x.GetByUserIdAsync(invitation.PatientUserId, default))
            .ReturnsAsync((PatientProfile?)null);
        _patientProfileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PatientProfile>(), default))
            .ReturnsAsync(new PatientProfile());
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(createdTherapist.Id, createdTherapist.Role.ToString(), createdTherapist.Email))
            .Returns(expectedToken);

        // Act
        var result = await _authenticationService.SetupTherapistAsync(request);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(createdTherapist.Id));
        Assert.That(result.Email, Is.EqualTo(createdTherapist.Email));
        Assert.That(result.Username, Is.EqualTo(createdTherapist.Username));
        Assert.That(result.Token, Is.EqualTo(expectedToken));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
        _therapistAccessRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TherapistAccess>(), default), Times.Once);
    }

    [Test]
    public void SetupTherapistAsync_PasswordsDoNotMatch_ShouldThrowException()
    {
        // Arrange
        var request = new TherapistSetupRequest
        {
            Token = "invitation-token",
            Username = "therapistuser",
            Password = "password123",
            ConfirmPassword = "differentpassword"
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.SetupTherapistAsync(request));
        Assert.That(exception.Message, Is.EqualTo("Passwords do not match"));
    }

    [Test]
    public void SetupTherapistAsync_InvalidToken_ShouldThrowException()
    {
        // Arrange
        var request = new TherapistSetupRequest
        {
            Token = "invalid-token",
            Username = "therapistuser",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        _invitationRepositoryMock.Setup(x => x.GetByTokenAsync(request.Token, default))
            .ReturnsAsync((TherapistInvitation?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            () => _authenticationService.SetupTherapistAsync(request));
        Assert.That(exception.Message, Is.EqualTo("Invalid or expired therapist invitation token"));
    }
}