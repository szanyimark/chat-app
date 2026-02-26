using ChatApp.Backend.Configuration;
using ChatApp.Backend.Models;
using ChatApp.Backend.Services;
using Xunit;

namespace ChatApp.Backend.Tests;

public class JwtServiceTests
{
    private readonly AppConfig _config;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _config = new AppConfig
        {
            JwtKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
            JwtIssuer = "ChatApp",
            JwtAudience = "ChatApp",
            JwtExpiryMinutes = 60
        };
        _jwtService = new JwtService(_config);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT format has dots
    }

    [Fact]
    public void GenerateToken_ShouldContainUserClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(userId.ToString(), jwtToken.Subject);
        Assert.Equal("test@example.com", jwtToken.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("testuser", jwtToken.Claims.First(c => c.Type == "username").Value);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("ChatApp", jwtToken.Issuer);
        Assert.Contains("ChatApp", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiry()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        var actualExpiry = jwtToken.ValidTo;

        // Allow 1 minute tolerance
        var timeDifference = Math.Abs((expectedExpiry - actualExpiry).TotalMinutes);
        Assert.True(timeDifference < 1, $"Expected expiry around {expectedExpiry}, got {actualExpiry}");
    }

    [Fact]
    public void GetTokenExpiry_WithValidToken_ShouldReturnExpiryDate()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser"
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var expiry = _jwtService.GetTokenExpiry(token);

        // Assert
        Assert.NotNull(expiry);
        Assert.True(expiry > DateTime.UtcNow);
    }

    [Fact]
    public void GetTokenExpiry_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var expiry = _jwtService.GetTokenExpiry(invalidToken);

        // Assert
        Assert.Null(expiry);
    }

    [Fact]
    public void GetTokenExpiry_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var emptyToken = "";

        // Act
        var expiry = _jwtService.GetTokenExpiry(emptyToken);

        // Assert
        Assert.Null(expiry);
    }
}