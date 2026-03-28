using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SphereBlog.Tests;

public class BlogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BlogTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthToken()
    {
        var email = $"blog-{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = email,
                Password = "Password123!",
                DisplayName = "Blog Author",
            }
        );

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    private void SetAuth(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    [Fact]
    public async Task GetAllBlogs_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/blogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("success", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateBlog_Authenticated_ReturnsCreated()
    {
        var token = await GetAuthToken();
        SetAuth(token);

        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                Title = "Test Blog",
                Content = "This is a test blog post.",
                TagNames = new[] { "csharp", "dotnet" },
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test Blog", body.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal(2, body.GetProperty("data").GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public async Task CreateBlog_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            "/api/blogs",
            new { Title = "Test Blog", Content = "Content" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBlogById_ReturnsFullBlog()
    {
        var token = await GetAuthToken();
        SetAuth(token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                Title = "Detail Blog",
                Content = "Full content here.",
                TagNames = new[] { "test" },
            }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blogId = created.GetProperty("data").GetProperty("id").GetString();

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Detail Blog", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateBlog_AsOwner_ReturnsUpdated()
    {
        var token = await GetAuthToken();
        SetAuth(token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                Title = "Original",
                Content = "Original content.",
                TagNames = new[] { "original" },
            }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blogId = created.GetProperty("data").GetProperty("id").GetString();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/blogs/{blogId}",
            new
            {
                Title = "Updated",
                Content = "Updated content.",
                TagNames = new[] { "updated" },
            }
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var body = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeleteBlog_AsOwner_Returns204()
    {
        var token = await GetAuthToken();
        SetAuth(token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/blogs",
            new { Title = "To Delete", Content = "Will be deleted." }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blogId = created.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBlog_NonOwner_Returns403()
    {
        var ownerToken = await GetAuthToken();
        SetAuth(ownerToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/blogs",
            new { Title = "Owner Only", Content = "Cannot be deleted by others." }
        );

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blogId = created.GetProperty("data").GetProperty("id").GetString();

        var otherToken = await GetAuthToken();
        SetAuth(otherToken);

        var deleteResponse = await _client.DeleteAsync($"/api/blogs/{blogId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }
}
