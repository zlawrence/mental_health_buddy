using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
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
}