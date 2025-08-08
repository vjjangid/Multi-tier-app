using AuthService.Services;
using KanbanBoard.Common.Responses;
using KanbanBoard.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> Register([FromBody] UserRegistrationDto registrationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<UserResponseDto>.ErrorResponse("Validation failed", errors, 400));
            }

            var user = await _authService.RegisterAsync(registrationDto);
            if (user == null)
            {
                return BadRequest(ApiResponse<UserResponseDto>.ErrorResponse("Registration failed", statusCode: 400));
            }

            return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user, "User registered successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserResponseDto>.ErrorResponse(ex.Message, statusCode: 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, ApiResponse<UserResponseDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", errors, 400));
            }

            var token = await _authService.LoginAsync(loginDto);
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid username or password", statusCode: 401));
            }

            var user = await _authService.GetUserByUsernameAsync(loginDto.Username);
            
            var response = new
            {
                Token = token,
                User = user
            };

            return Ok(ApiResponse<object>.SuccessResponse(response, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpPost("guest")]
    public async Task<ActionResult<ApiResponse<object>>> CreateGuestUser()
    {
        try
        {
            var guestUser = await _authService.CreateGuestUserAsync();
            if (guestUser == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Failed to create guest user", statusCode: 400));
            }

            var token = _authService.GenerateJwtToken(new User
            {
                Id = guestUser.Id,
                Username = guestUser.Username,
                Email = guestUser.Email,
                FullName = guestUser.FullName,
                IsGuest = guestUser.IsGuest,
                CreatedAt = guestUser.CreatedAt
            });

            var response = new
            {
                Token = token,
                User = guestUser
            };

            return Ok(ApiResponse<object>.SuccessResponse(response, "Guest user created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest user");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<object>>> ValidateToken()
    {
        try
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("No token provided", statusCode: 401));
            }

            var isValid = await _authService.ValidateTokenAsync(token);
            
            if (!isValid)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token", statusCode: 401));
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { Valid = true }, "Token is valid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUser(Guid userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(ApiResponse<UserResponseDto>.ErrorResponse("User not found", statusCode: 404));
            }

            return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user, "User retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID: {UserId}", userId);
            return StatusCode(500, ApiResponse<UserResponseDto>.ErrorResponse("Internal server error", statusCode: 500));
        }
    }
}