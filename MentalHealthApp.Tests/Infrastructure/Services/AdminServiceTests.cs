using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using MentalHealthApp.Domain.Entities;
using MentalHealthApp.Domain.Repositories;
using MentalHealthApp.Infrastructure.Services;
using Moq;
using NUnit.Framework;

namespace MentalHealthApp.Tests.Infrastructure.Services;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private Mock<IPasswordHashingService> _passwordHashingServiceMock;
    private AdminService _service;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _passwordHashingServiceMock = new Mock<IPasswordHashingService>();

        _service = new AdminService(
            _userRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _passwordHashingServiceMock.Object);

        _auditLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<AuditLog>(), default))
            .ReturnsAsync(new AuditLog());
    }

    // ─── GetAllUsersAsync ────────────────────────────────────────────────────

    [Test]
    public async Task GetAllUsers_ReturnsUserManagementResponses()
    {
        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(default))
            .ReturnsAsync(new List<User>
            {
                new User { Id = "u1", Email = "a@test.com", Username = "alice", Role = UserRole.Patient, IsActive = true },
                new User { Id = "u2", Email = "b@test.com", Username = "bob", Role = UserRole.Therapist, IsActive = true }
            });

        var result = await _service.GetAllUsersAsync();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].UserId, Is.EqualTo("u1"));
        Assert.That(result[0].Role, Is.EqualTo("Patient"));
        Assert.That(result[1].Role, Is.EqualTo("Therapist"));
    }

    [Test]
    public async Task GetAllUsers_EmptyList_ReturnsEmpty()
    {
        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(default))
            .ReturnsAsync(new List<User>());

        var result = await _service.GetAllUsersAsync();

        Assert.That(result, Is.Empty);
    }

    // ─── ResetPasswordAsync ──────────────────────────────────────────────────

    [Test]
    public async Task ResetPassword_ValidRequest_UpdatesHashAndLogsAudit()
    {
        var user = new User { Id = "user1", Role = UserRole.Patient };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("user1", default))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);

        _passwordHashingServiceMock
            .Setup(x => x.HashPassword("NewPass1!"))
            .Returns("hashed-password");

        await _service.ResetPasswordAsync(new ResetPasswordRequest
        {
            UserId = "user1",
            Password = "NewPass1!",
            ConfirmPassword = "NewPass1!"
        }, adminId: "admin1");

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.PasswordHash == "hashed-password"), default),
            Times.Once);

        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<AuditLog>(l =>
                l.AdminId == "admin1" &&
                l.Action == "ResetPassword" &&
                l.TargetId == "user1"), default),
            Times.Once);
    }

    [Test]
    public void ResetPassword_PasswordMismatch_ThrowsInvalidOperation()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = "user1",
                Password = "Pass1",
                ConfirmPassword = "Pass2"
            }, adminId: "admin1"));

        Assert.That(ex.Message, Is.EqualTo("Passwords do not match"));
    }

    [Test]
    public void ResetPassword_UserNotFound_ThrowsInvalidOperation()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((User?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = "missing",
                Password = "Pass1!",
                ConfirmPassword = "Pass1!"
            }, adminId: "admin1"));

        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    // ─── ChangePatientStatusAsync ────────────────────────────────────────────

    [Test]
    public async Task ChangePatientStatus_Activate_UpdatesUserAndLogs()
    {
        var patient = new User { Id = "p1", Role = UserRole.Patient, IsActive = false };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("p1", default))
            .ReturnsAsync(patient);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);

        await _service.ChangePatientStatusAsync(new ChangePatientStatusRequest
        {
            PatientId = "p1",
            IsActive = true,
            Reason = "Reinstated by admin"
        }, adminId: "admin1");

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<User>(u => u.IsActive == true), default),
            Times.Once);

        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<AuditLog>(l =>
                l.Action == "ActivatePatient" &&
                l.TargetId == "p1"), default),
            Times.Once);
    }

    [Test]
    public async Task ChangePatientStatus_Deactivate_LogsDeactivate()
    {
        var patient = new User { Id = "p1", Role = UserRole.Patient, IsActive = true };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("p1", default))
            .ReturnsAsync(patient);

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);

        await _service.ChangePatientStatusAsync(new ChangePatientStatusRequest
        {
            PatientId = "p1",
            IsActive = false,
            Reason = "Violated terms"
        }, adminId: "admin1");

        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<AuditLog>(l => l.Action == "DeactivatePatient"), default),
            Times.Once);
    }

    [Test]
    public void ChangePatientStatus_UserNotPatient_ThrowsInvalidOperation()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("u1", default))
            .ReturnsAsync(new User { Id = "u1", Role = UserRole.Therapist });

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangePatientStatusAsync(new ChangePatientStatusRequest
            {
                PatientId = "u1",
                IsActive = true
            }, adminId: "admin1"));
    }

    [Test]
    public void ChangePatientStatus_UserNotFound_ThrowsInvalidOperation()
    {
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync("missing", default))
            .ReturnsAsync((User?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangePatientStatusAsync(new ChangePatientStatusRequest
            {
                PatientId = "missing",
                IsActive = true
            }, adminId: "admin1"));
    }

    // ─── AddAdminAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task AddAdmin_ValidRequest_CreatesAdminAndLogs()
    {
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("newadmin", default))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("newadmin@test.com", default))
            .ReturnsAsync((User?)null);

        _passwordHashingServiceMock
            .Setup(x => x.HashPassword("AdminPass1!"))
            .Returns("hashed");

        var created = new User
        {
            Id = "admin2",
            Email = "newadmin@test.com",
            Username = "newadmin",
            Role = UserRole.Admin,
            IsActive = true
        };

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(created);

        var result = await _service.AddAdminAsync(new AddAdminRequest
        {
            Username = "newadmin",
            Email = "newadmin@test.com",
            Password = "AdminPass1!",
            ConfirmPassword = "AdminPass1!"
        }, adminId: "admin1");

        Assert.That(result.UserId, Is.EqualTo("admin2"));
        Assert.That(result.Role, Is.EqualTo("Admin"));

        _auditLogRepositoryMock.Verify(
            x => x.AddAsync(It.Is<AuditLog>(l =>
                l.AdminId == "admin1" &&
                l.Action == "CreateAdmin"), default),
            Times.Once);
    }

    [Test]
    public void AddAdmin_PasswordMismatch_ThrowsInvalidOperation()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAdminAsync(new AddAdminRequest
            {
                Username = "admin",
                Email = "admin@test.com",
                Password = "Pass1",
                ConfirmPassword = "Pass2"
            }, adminId: "admin1"));

        Assert.That(ex.Message, Is.EqualTo("Passwords do not match"));
    }

    [Test]
    public void AddAdmin_DuplicateUsername_ThrowsInvalidOperation()
    {
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("existing", default))
            .ReturnsAsync(new User { Username = "existing" });

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAdminAsync(new AddAdminRequest
            {
                Username = "existing",
                Email = "new@test.com",
                Password = "Pass1!",
                ConfirmPassword = "Pass1!"
            }, adminId: "admin1"));

        Assert.That(ex.Message, Does.Contain("already taken").IgnoreCase);
    }

    // ─── GetAuditLogsAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetAuditLogs_WithDateRange_CallsGetByDateRange()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        _auditLogRepositoryMock
            .Setup(x => x.GetByDateRangeAsync(from, to, default))
            .ReturnsAsync(new List<AuditLog>
            {
                new AuditLog { Id = "log1", Action = "ResetPassword", AdminId = "admin1" }
            });

        var result = await _service.GetAuditLogsAsync(from, to);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Action, Is.EqualTo("ResetPassword"));

        _auditLogRepositoryMock.Verify(x => x.GetByDateRangeAsync(from, to, default), Times.Once);
        _auditLogRepositoryMock.Verify(x => x.GetAllAsync(default), Times.Never);
    }

    [Test]
    public async Task GetAuditLogs_NullDates_CallsGetAll()
    {
        _auditLogRepositoryMock
            .Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new List<AuditLog>
            {
                new AuditLog { Id = "log1", Action = "CreateAdmin", AdminId = "admin1" }
            });

        var result = await _service.GetAuditLogsAsync(null, null);

        Assert.That(result.Count, Is.EqualTo(1));
        _auditLogRepositoryMock.Verify(x => x.GetAllAsync(default), Times.Once);
    }

    [Test]
    public async Task GetAuditLogs_MapsAllFields()
    {
        var ts = DateTime.UtcNow;
        _auditLogRepositoryMock
            .Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new List<AuditLog>
            {
                new AuditLog
                {
                    Id = "log1",
                    AdminId = "admin1",
                    Action = "DeactivatePatient",
                    TargetId = "p1",
                    TargetType = "Patient",
                    Details = "reason",
                    Timestamp = ts
                }
            });

        var result = await _service.GetAuditLogsAsync(null, null);

        var log = result[0];
        Assert.That(log.AdminId, Is.EqualTo("admin1"));
        Assert.That(log.Action, Is.EqualTo("DeactivatePatient"));
        Assert.That(log.TargetId, Is.EqualTo("p1"));
        Assert.That(log.TargetType, Is.EqualTo("Patient"));
        Assert.That(log.Details, Is.EqualTo("reason"));
        Assert.That(log.Timestamp, Is.EqualTo(ts));
    }
}
