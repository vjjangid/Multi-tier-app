using System;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Services;
using KanbanBoard.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly AuthDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuthDbContext(options);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();

        _mockConfiguration.Setup(x => x["Jwt:Secret"])
            .Returns("this-is-a-super-secret-key-for-jwt-token-generation-minimum-256-bits");

        _authService = new AuthenticationService(_context, _mockConfiguration.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsUserResponse()
    {
        var registrationDto = new UserRegistrationDto
        {
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var result = await _authService.RegisterAsync(registrationDto);

        Assert.NotNull(result);
        Assert.Equal(registrationDto.Username, result.Username);
        Assert.Equal(registrationDto.Email, result.Email);
        Assert.Equal(registrationDto.FullName, result.FullName);
        Assert.False(result.IsGuest);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == registrationDto.Username);
        Assert.NotNull(userInDb);
        Assert.Equal(registrationDto.Username, userInDb.Username);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsArgumentException()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "existing@example.com",
            FullName = "Existing User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registrationDto = new UserRegistrationDto
        {
            Username = "testuser",
            Email = "new@example.com",
            FullName = "New User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registrationDto));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsArgumentException()
    {
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "existinguser",
            Email = "test@example.com",
            FullName = "Existing User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registrationDto = new UserRegistrationDto
        {
            Username = "newuser",
            Email = "test@example.com",
            FullName = "New User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(registrationDto));
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_StoresPasswordHash()
    {
        var registrationDto = new UserRegistrationDto
        {
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var result = await _authService.RegisterAsync(registrationDto);
        Assert.NotNull(result);

        var passwordRecord = await _context.UserPasswords
            .FirstOrDefaultAsync(up => up.UserId == result.Id);
        Assert.NotNull(passwordRecord);
        Assert.NotEmpty(passwordRecord.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(registrationDto.Password, passwordRecord.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var userPassword = new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserPasswords.Add(userPassword);
        await _context.SaveChangesAsync();

        var loginDto = new UserLoginDto
        {
            Username = "testuser",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentialsWithEmail_ReturnsToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var userPassword = new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserPasswords.Add(userPassword);
        await _context.SaveChangesAsync();

        var loginDto = new UserLoginDto
        {
            Username = "test@example.com",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ReturnsNull()
    {
        var loginDto = new UserLoginDto
        {
            Username = "nonexistent",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var userPassword = new UserPassword
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserPasswords.Add(userPassword);
        await _context.SaveChangesAsync();

        var loginDto = new UserLoginDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateGuestUserAsync_ValidCall_ReturnsGuestUser()
    {
        var result = await _authService.CreateGuestUserAsync();

        Assert.NotNull(result);
        Assert.True(result.IsGuest);
        Assert.StartsWith("Guest_", result.Username);
        Assert.Contains("@kanban.app", result.Email);
        Assert.Equal("Guest User", result.FullName);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        Assert.NotNull(userInDb);
        Assert.True(userInDb.IsGuest);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _authService.GetUserByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _authService.GetUserByIdAsync(nonExistentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ExistingUser_ReturnsUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _authService.GetUserByUsernameAsync("testuser");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _authService.GetUserByUsernameAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        var result = await _authService.ValidateTokenAsync("valid-token");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ReturnsFalse()
    {
        var result = await _authService.ValidateTokenAsync("");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_NullToken_ReturnsFalse()
    {
        var result = await _authService.ValidateTokenAsync(null!);

        Assert.False(result);
    }

    [Fact]
    public void GenerateJwtToken_ValidUser_ReturnsToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            IsGuest = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = _authService.GenerateJwtToken(user);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        
        Assert.Equal(user.Id.ToString(), token.Claims.First(x => x.Type == "userId").Value);
        Assert.Equal(user.Username, token.Claims.First(x => x.Type == "username").Value);
        Assert.Equal(user.Email, token.Claims.First(x => x.Type == "email").Value);
        Assert.Equal(user.IsGuest.ToString(), token.Claims.First(x => x.Type == "isGuest").Value);
    }

    [Fact]
    public void GenerateJwtToken_GuestUser_ReturnsTokenWithGuestFlag()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "Guest_20240101000000",
            Email = "guest_20240101000000@kanban.app",
            FullName = "Guest User",
            IsGuest = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = _authService.GenerateJwtToken(user);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        
        Assert.Equal("True", token.Claims.First(x => x.Type == "isGuest").Value);
    }
}