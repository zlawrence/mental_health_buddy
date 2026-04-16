using MentalHealthApp.Application.DTOs;
using MentalHealthApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MentalHealthApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
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

    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            var patientId = GetCurrentUserId();
            var result = await _conversationService.CreateConversationAsync(patientId, request);
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

    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        try
        {
            var patientId = GetCurrentUserId();
            var result = await _conversationService.GetPatientConversationsAsync(patientId);
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

    [HttpGet("me")]
    public async Task<IActionResult> GetMyConversations()
    {
        return await GetConversations();
    }

    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId)
    {
        try
        {
            var patientId = GetCurrentUserId();
            var result = await _conversationService.GetConversationAsync(conversationId);
            if (result.PatientId != patientId)
            {
                return Forbid();
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var patientId = GetCurrentUserId();
            var result = await _conversationService.SendMessageAsync(patientId, request);
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

    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(string conversationId)
    {
        try
        {
            var patientId = GetCurrentUserId();
            var conversation = await _conversationService.GetConversationAsync(conversationId);
            if (conversation.PatientId != patientId)
            {
                return Forbid();
            }

            var result = await _conversationService.GetConversationMessagesAsync(conversationId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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

    [HttpPost("{conversationId}/archive")]
    public async Task<IActionResult> ArchiveConversation(string conversationId)
    {
        try
        {
            var patientId = GetCurrentUserId();
            var conversation = await _conversationService.GetConversationAsync(conversationId);
            if (conversation.PatientId != patientId)
            {
                return Forbid();
            }

            await _conversationService.UpdateConversationAsync(conversationId, true);
            return Ok(new { message = "Conversation archived successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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