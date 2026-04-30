using System.Security.Claims;
using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpGet("check-availability")]
    public async Task<IActionResult> CheckAvailability([FromQuery] string? username, [FromQuery] string? email)
    {
        try
        {
            var result = await _authService.CheckAvailabilityAsync(username, email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register-patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientRequest request)
    {
        try
        {
            var result = await _authService.RegisterPatientAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login-patient")]
    public async Task<IActionResult> LoginPatient([FromBody] PatientLoginRequest request)
    {
        try
        {
            var result = await _authService.LoginPatientAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("setup-therapist")]
    public async Task<IActionResult> SetupTherapist([FromBody] TherapistSetupRequest request)
    {
        try
        {
            var result = await _authService.SetupTherapistAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login-therapist")]
    public async Task<IActionResult> LoginTherapist([FromBody] TherapistLoginRequest request)
    {
        try
        {
            var result = await _authService.LoginTherapistAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login-admin")]
    public async Task<IActionResult> LoginAdmin([FromBody] AdminLoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAdminAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("validate-invitation")]
    public async Task<IActionResult> ValidateInvitation([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token is required" });

        var result = await _authService.ValidateInvitationAsync(token);
        return Ok(result);
    }

    [Authorize(Roles = "Therapist")]
    [HttpPost("claim-invitation")]
    public async Task<IActionResult> ClaimInvitation([FromBody] ClaimInvitationRequest request)
    {
        try
        {
            var therapistUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(therapistUserId))
                return Unauthorized(new { message = "Invalid token" });

            await _authService.ClaimInvitationAsync(request.Token, therapistUserId);
            return Ok(new { message = "Invitation claimed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}