using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SphereBlog.Tests;

public class CommentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CommentTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string BlogId)> CreateUserAndBlog()
    {
        var email = $"comment-{Guid.NewGuid()}@example.com";
        var regResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = email,
                Password = "Password123!",
                DisplayName = "Commenter",
            }
        );

        var regBody = await regResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = regBody.GetProperty("data").GetProperty("token").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var blogResponse = await _client.PostAsJsonAsync(
            "/api/blogs",
            new { Title = "Blog for Comments", Content = "Content here." }
        );

        var blogBody = await blogResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blogId = blogBody.GetProperty("data").GetProperty("id").GetString()!;

        return (token, blogId);
    }

    [Fact]
    public async Task CreateComment_Authenticated_Returns201()
    {
        var (token, blogId) = await CreateUserAndBlog();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var response = await _client.PostAsJsonAsync(
            $"/api/blogs/{blogId}/comments",
            new { Body = "Great blog post!" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Great blog post!", body.GetProperty("data").GetProperty("body").GetString());
    }

    [Fact]
    public async Task CreateComment_Unauthenticated_Returns401()
    {
        var (_, blogId) = await CreateUserAndBlog();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            $"/api/blogs/{blogId}/comments",
            new { Body = "Should fail" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_AsOwner_Returns204()
    {
        var (token, blogId) = await CreateUserAndBlog();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/blogs/{blogId}/comments",
            new { Body = "To be deleted" }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await _client.DeleteAsync($"/api/blogs/{blogId}/comments/{commentId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_NonOwner_Returns403()
    {
        var (ownerToken, blogId) = await CreateUserAndBlog();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ownerToken
        );

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/blogs/{blogId}/comments",
            new { Body = "Owner's comment" }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var commentId = created.GetProperty("data").GetProperty("id").GetString();

        // Register a different user
        var otherRegResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = $"other-{Guid.NewGuid()}@example.com",
                Password = "Password123!",
                DisplayName = "Other User",
            }
        );

        var otherBody = await otherRegResponse.Content.ReadFromJsonAsync<JsonElement>();
        var otherToken = otherBody.GetProperty("data").GetProperty("token").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            otherToken
        );

        var deleteResponse = await _client.DeleteAsync($"/api/blogs/{blogId}/comments/{commentId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }
}
