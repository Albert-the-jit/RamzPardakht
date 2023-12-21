// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.IntegrationTests;

    public class GeneralWebTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _customWebApplicationFactory;

    public GeneralWebTests(CustomWebApplicationFactory customWebApplicationFactory)
    {
        _customWebApplicationFactory = customWebApplicationFactory;
    }

    [Fact]
    public async Task Test_Admin_User_Created_At_Application_Start()
    {
        await Task.Delay(1000);
        var factory = _customWebApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers()
                    .AddApplicationPart(GetType().Assembly))); // add TestController to app Controller

        var serviceProvider = factory.Services.CreateScope().ServiceProvider;

        var client = factory.CreateClient();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var loginModel = new LoginRequest()
        {
            Email = configuration["Admin:Email"],
            Password = configuration["Admin:Password"]
        };

        var loginRequest = await client.PostAsJsonAsync("/v1/Account/Login", loginModel);
        loginRequest.EnsureSuccessStatusCode();
        var loginResultModel = await loginRequest.Content.ReadFromJsonAsync<AccessTokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResultModel.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, "v1/Test/UserDetail");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var readFromString = await response.Content.ReadFromJsonAsync<string>();

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        var adminUser = await userManager.FindByNameAsync(loginModel.Email);
        var isInAdminRole = await userManager.IsInRoleAsync(adminUser, SystemConst.AdminRoleName);

        isInAdminRole.Should().BeTrue();
        readFromString.Should().NotBeNullOrWhiteSpace();
        loginResultModel.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Test_HttpContext_User_Identity_Name_fill_correctly_with_SUB_from_Bearer_token()
    {
        var factory = _customWebApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers()
                    .AddApplicationPart(GetType().Assembly))); // add TestController to app Controller


        var serviceProvider = factory.Services.CreateScope().ServiceProvider;

        var client = factory.CreateClient();

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        var newEmail = "a@a.com";
        var newPass = "#Uhsdf234235";
        var registerModel = new RegisterRequest()
        {
            Email = newEmail,
            Password = newPass
        };


        var registerResult = await client.PostAsJsonAsync("/v1/Account/Register", registerModel);
        registerResult.EnsureSuccessStatusCode();

        var user = await userManager.FindByEmailAsync(newEmail);
        user!.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        var loginModel = new LoginRequest()
        {
            Email = newEmail,
            Password = newPass
        };

        var loginRequest = await client.PostAsJsonAsync("/v1/Account/Login", loginModel);
        loginRequest.EnsureSuccessStatusCode();

        var loginResultModel = await loginRequest.Content.ReadFromJsonAsync<AccessTokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResultModel.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Get, "v1/Test/UserDetail");



        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var readFromString = await response.Content.ReadFromJsonAsync<string>();

        readFromString.Should().BeEquivalentTo(newEmail);
    }

    [Fact]
    public async Task Test_Default_Culture_On_RequestLocalization()
    {
        var factory = _customWebApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers()
                    .AddApplicationPart(GetType().Assembly))); // add TestController to app Controller
        // Arrange
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "v1/Test/Culture");

        // Act
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Test-en");
    }

    [Theory]
    [InlineData("en-US", "Test-en")]
    [InlineData("fa-IR", "Test-fa")]
    public async Task Test_Cultures_On_RequestLocalization(string acceptLanguageHeader, string expectedValue)
    {
        var factory = _customWebApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddControllers()
                    .AddApplicationPart(GetType().Assembly))); // add TestController to app Controller
        // Arrange
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(acceptLanguageHeader));

        var request = new HttpRequestMessage(HttpMethod.Get, "v1/Test/Culture");

        // Act
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain(expectedValue);
    }
}
