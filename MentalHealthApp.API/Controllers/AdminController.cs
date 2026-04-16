using System.Security.Claims;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var adminId = GetCurrentUserId();
        try
        {
            await _adminService.ResetPasswordAsync(request, adminId);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-patient-status")]
    public async Task<IActionResult> ChangePatientStatus([FromBody] ChangePatientStatusRequest request)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var adminId = GetCurrentUserId();
        try
        {
            await _adminService.ChangePatientStatusAsync(request, adminId);
            return Ok(new { message = "Patient status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("add-admin")]
    public async Task<IActionResult> AddAdmin([FromBody] AddAdminRequest request)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var adminId = GetCurrentUserId();
        try
        {
            var response = await _adminService.AddAdminAsync(request, adminId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!IsCurrentUserAdmin())
        {
            return Forbid();
        }

        var logs = await _adminService.GetAuditLogsAsync(from, to);
        return Ok(logs);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private bool IsCurrentUserAdmin()
    {
        return User.FindFirstValue(ClaimTypes.Role)?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
    }
}
