using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Xunit;

namespace InternTracker.Tests;

public class AdminRegistrationTests
{
    [Theory]
    [InlineData("Production", false, HttpStatusCode.NotFound)]
    [InlineData("Production", true, HttpStatusCode.NotFound)]
    [InlineData("Development", false, HttpStatusCode.NotFound)]
    [InlineData("Development", true, HttpStatusCode.OK)]
    public async Task RegistrationRequiresDevelopmentAndExplicitOptIn(
        string environment, bool enabled, HttpStatusCode expected)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Demo:EnableAdminRegistration"] = enabled.ToString()
                    }));
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Account/RegisterAdmin");

        Assert.Equal(expected, response.StatusCode);

        var registrationPage = await client.GetStringAsync("/Account/GetStarted");
        var token = Regex.Match(registrationPage,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token.Groups[1].Value
        });
        var post = await client.PostAsync("/Account/RegisterAdmin", form);
        Assert.Equal(expected, post.StatusCode);
    }

    [Theory]
    [InlineData("ChangePassword")]
    [InlineData("UpdateEmail")]
    [InlineData("UploadProfilePicture")]
    public async Task ProfileActionsRequireAuthentication(string action)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync($"/Profile/{action}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://localhost/Account/Login", response.Headers.Location?.ToString());
    }
}
