using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User identifier claim is missing.");
        }

        return userId;
    }

    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _patientService.GetPatientProfileAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdatePatientProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _patientService.UpdatePatientProfileAsync(userId, request);
            return Ok(new { message = "Profile updated successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/guard-rails")]
    public async Task<IActionResult> GetMyGuardRails()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _patientService.GetPatientGuardRailsAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/guard-rails")]
    public async Task<IActionResult> CreateGuardRail([FromBody] CreateGuardRailRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _patientService.CreateGuardRailAsync(userId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("guard-rails/{guardRailId}")]
    public async Task<IActionResult> UpdateGuardRail(string guardRailId, [FromBody] UpdateGuardRailRequest request)
    {
        try
        {
            await _patientService.UpdateGuardRailAsync(guardRailId, request);
            return Ok(new { message = "Guard rail updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("guard-rails/{guardRailId}")]
    public async Task<IActionResult> DeleteGuardRail(string guardRailId)
    {
        try
        {
            await _patientService.DeleteGuardRailAsync(guardRailId);
            return Ok(new { message = "Guard rail deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/invite-therapist")]
    public async Task<IActionResult> InviteTherapist([FromBody] InviteTherapistRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _patientService.InviteTherapistAsync(userId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}