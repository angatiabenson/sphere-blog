using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SphereBlog.Tests;

public class TagTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TagTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthToken()
    {
        var email = $"tag-{Guid.NewGuid()}@example.com";
        var regResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = email,
                Password = "Password123!",
                DisplayName = "Tag Tester",
            }
        );

        var body = await regResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task GetAllTags_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("success", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetBlogsByTag_ReturnsFilteredResults()
    {
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );

        await _client.PostAsJsonAsync(
            "/api/blogs",
            new
            {
                Title = "Tagged Blog",
                Content = "Content with tags.",
                TagNames = new[] { "unique-tag-test" },
            }
        );

        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tags/unique-tag-test/blogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("data").GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);
    }
}
