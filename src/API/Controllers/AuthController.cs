using Microsoft.AspNetCore.Mvc;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Auth;
using SphereBlog.Application.Interfaces;

namespace SphereBlog.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await authService.RegisterAsync(request);
            return StatusCode(201, ApiResponse<AuthResponse>.Success(result, 201));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Error(ex.Message, 400, CorrelationId));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await authService.LoginAsync(request);
            return Ok(ApiResponse<AuthResponse>.Success(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Error(ex.Message, 400, CorrelationId));
        }
    }

    private string CorrelationId =>
        HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
}
