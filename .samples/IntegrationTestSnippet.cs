using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/dossier/1")]
    [InlineData("/api/me")]
    public async Task Get_Endpoints_ReturnUnauthorized_ForAnonymousUser(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Response_Should_Contain_Security_Headers()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/security-check");

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }
}