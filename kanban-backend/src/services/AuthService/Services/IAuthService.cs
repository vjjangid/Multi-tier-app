using KanbanBoard.Shared.Models;
using AuthService.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public interface IAuthService
{
    Task<UserResponseDto?> RegisterAsync(UserRegistrationDto registrationDto);
    Task<string?> LoginAsync(UserLoginDto loginDto);
    Task<UserResponseDto?> CreateGuestUserAsync();
    Task<UserResponseDto?> GetUserByIdAsync(Guid userId);
    Task<UserResponseDto?> GetUserByUsernameAsync(string username);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateJwtToken(User user);
}

public class AuthenticationService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        AuthDbContext context, 
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UserResponseDto?> RegisterAsync(UserRegistrationDto registrationDto)
    {
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Username == registrationDto.Username))
            {
                throw new ArgumentException("Username already exists");
            }

            if (await _context.Users.AnyAsync(u => u.Email == registrationDto.Email))
            {
                throw new ArgumentException("Email already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = registrationDto.Username,
                Email = registrationDto.Email,
                FullName = registrationDto.FullName,
                IsGuest = false,
                CreatedAt = DateTime.UtcNow
            };

            // Hash password and store it separately (in a real app, use proper password hashing)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password);
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Store password hash in a separate table or service
            await StorePasswordHashAsync(user.Id, hashedPassword);

            _logger.LogInformation("User registered successfully: {Username}", user.Username);

            return MapToUserResponseDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", registrationDto.Username);
            throw;
        }
    }

    public async Task<string?> LoginAsync(UserLoginDto loginDto)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

            if (user == null)
            {
                return null;
            }

            var storedHash = await GetPasswordHashAsync(user.Id);
            if (string.IsNullOrEmpty(storedHash) || !BCrypt.Net.BCrypt.Verify(loginDto.Password, storedHash))
            {
                return null;
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("User logged in successfully: {Username}", user.Username);
            
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", loginDto.Username);
            throw;
        }
    }

    public async Task<UserResponseDto?> CreateGuestUserAsync()
    {
        try
        {
            var guestUser = new User
            {
                Id = Guid.NewGuid(),
                Username = $"Guest_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Email = $"guest_{DateTime.UtcNow:yyyyMMddHHmmss}@kanban.app",
                FullName = "Guest User",
                IsGuest = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(guestUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Guest user created: {Username}", guestUser.Username);

            return MapToUserResponseDto(guestUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest user");
            throw;
        }
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? MapToUserResponseDto(user) : null;
    }

    public async Task<UserResponseDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user != null ? MapToUserResponseDto(user) : null;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            // Implement JWT token validation logic
            // This is a simplified version - in production, use proper JWT validation
            return Task.FromResult(!string.IsNullOrEmpty(token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Task.FromResult(false);
        }
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = System.Text.Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-super-secret-key-here");
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("userId", user.Id.ToString()),
                new System.Security.Claims.Claim("username", user.Username),
                new System.Security.Claims.Claim("email", user.Email),
                new System.Security.Claims.Claim("isGuest", user.IsGuest.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static UserResponseDto MapToUserResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Avatar = user.Avatar,
            IsGuest = user.IsGuest,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task StorePasswordHashAsync(Guid userId, string hashedPassword)
    {
        var userPassword = new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserPasswords.Add(userPassword);
        await _context.SaveChangesAsync();
    }

    private async Task<string?> GetPasswordHashAsync(Guid userId)
    {
        var userPassword = await _context.UserPasswords
            .FirstOrDefaultAsync(up => up.UserId == userId);
        
        return userPassword?.PasswordHash;
    }
}