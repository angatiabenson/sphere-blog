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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return StatusCode(201, ApiResponse<AuthResponse>.Success(result, 201));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result));
    }
}
