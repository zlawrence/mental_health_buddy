using System.Security.Claims;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Therapist")]
public class TherapistController : ControllerBase
{
    private readonly ITherapistService _therapistService;

    public TherapistController(ITherapistService therapistService)
    {
        _therapistService = therapistService;
    }

    [HttpGet("me/patients")]
    public async Task<IActionResult> GetAssignedPatients()
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        var patients = await _therapistService.GetAssignedPatientsAsync(therapistId);
        return Ok(patients);
    }

    [HttpGet("me/patients/{patientId}")]
    public async Task<IActionResult> GetPatientProfile(string patientId)
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        try
        {
            var profile = await _therapistService.GetPatientProfileAsync(therapistId, patientId);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/patients/{patientId}/guard-rails")]
    public async Task<IActionResult> GetPatientGuardRails(string patientId)
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        try
        {
            var guardRails = await _therapistService.GetPatientGuardRailsAsync(therapistId, patientId);
            return Ok(guardRails);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/patients/{patientId}/guard-rails/{guardRailId}")]
    public async Task<IActionResult> UpdateGuardRail(string patientId, string guardRailId, [FromBody] UpdateGuardRailRequest request)
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        try
        {
            await _therapistService.UpdateGuardRailAsync(therapistId, patientId, guardRailId, request);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("me/patients/{patientId}/guard-rails/{guardRailId}")]
    public async Task<IActionResult> DeleteGuardRail(string patientId, string guardRailId)
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        try
        {
            await _therapistService.DeleteGuardRailAsync(therapistId, patientId, guardRailId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/patients/{patientId}/conversations")]
    public async Task<IActionResult> GetPatientConversations(string patientId)
    {
        if (!IsCurrentUserTherapist())
        {
            return Forbid();
        }

        var therapistId = GetCurrentUserId();
        try
        {
            var conversations = await _therapistService.GetPatientConversationsAsync(therapistId, patientId);
            return Ok(conversations);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private bool IsCurrentUserTherapist()
    {
        return User.FindFirstValue(ClaimTypes.Role)?.Equals("Therapist", StringComparison.OrdinalIgnoreCase) == true;
    }
}
