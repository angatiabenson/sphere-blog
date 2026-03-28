using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SphereBlog.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = $"test-{Guid.NewGuid()}@example.com",
                Password = "Password123!",
                DisplayName = "Test User",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("success", body.GetProperty("status").GetString());
        Assert.NotEmpty(body.GetProperty("data").GetProperty("token").GetString()!);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var email = $"dup-{Guid.NewGuid()}@example.com";
        var payload = new
        {
            Email = email,
            Password = "Password123!",
            DisplayName = "User",
        };

        await _client.PostAsJsonAsync("/api/auth/register", payload);
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("error", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = "not-an-email",
                Password = "Password123!",
                DisplayName = "Test",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = $"test-{Guid.NewGuid()}@example.com",
                Password = "short",
                DisplayName = "Test",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email = $"login-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = email,
                Password = "Password123!",
                DisplayName = "Login User",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = "Password123!" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("success", body.GetProperty("status").GetString());
        Assert.NotEmpty(body.GetProperty("data").GetProperty("token").GetString()!);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns400()
    {
        var email = $"wrong-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = email,
                Password = "Password123!",
                DisplayName = "User",
            }
        );

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = "WrongPassword!" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
