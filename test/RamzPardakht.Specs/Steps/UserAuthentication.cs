using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.IntegrationTests;
using TechTalk.SpecFlow.Assist;

namespace RamzPardakht.Specs.Steps;

[Binding]
public class UserAuthentication
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _httpClient;
    private readonly CustomWebApplicationFactory _applicationFactory;

    public UserAuthentication(ScenarioContext scenarioContext, CustomWebApplicationFactory applicationFactory)
    {
        _scenarioContext = scenarioContext;
        _applicationFactory = applicationFactory;
        _httpClient = applicationFactory.CreateClient();
    }


    [When(@"""(.*)"" sends register request with the following information:")]
    public async Task WhenSendsRegisterRequestWithTheFollowingInformation(string p0, Table table)
    {
        using var scope = _applicationFactory.Services.CreateScope();

        var registerRequest = table.CreateInstance<RegisterRequest>();

        var scopedServices = scope.ServiceProvider;
        var emailSenderMock = scopedServices.GetRequiredService<Mock<IEmailSender<User>>>();

        emailSenderMock.Should().NotBeNull();

        string confirmationLink = null;
        string userEmail = null;


        emailSenderMock
            .Setup(e => e.SendConfirmationLinkAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<User, string, string>((_, email, link) =>
            {
                confirmationLink = HttpUtility.HtmlDecode(link);
                userEmail = email;
            }).Returns(Task.CompletedTask);

        var request = await _httpClient.PostAsJsonAsync("/v1/Account/Register", registerRequest);

        _scenarioContext.Set(confirmationLink, $"{p0}:EmailConfirmationLink");
        _scenarioContext.Set(userEmail, $"{p0}:EmailConfirmationUserEmail");
        _scenarioContext.Set(registerRequest, $"{p0}:{registerRequest.GetType().Name}");
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [Then(@"the ""(.*)"" should receive a success message confirming success")]
    public void ThenTheShouldReceiveASuccessMessageConfirmingSuccess(string user)
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{user}:{nameof(HttpResponseMessage)}");
        res.EnsureSuccessStatusCode();
    }

    [Then(@"'(.*)' should receive email contains verify email link")]
    public async Task ThenShouldReceiveEmailContainsVerifyEmailLink(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");
        var confirmationLink = _scenarioContext.Get<string>($"{p0}:EmailConfirmationLink");
        var userEmail = _scenarioContext.Get<string>($"{p0}:EmailConfirmationUserEmail");

        confirmationLink.Should().NotBeNull();
        userEmail.Should().Be(registerModel.Email);
    }

    [Then(@"the ""(.*)"" should receive a failed message with ""(.*)"" status and ""(.*)"" error massage")]
    public async Task ThenTheShouldReceiveAFailedMessageWithStatusAndErrorMassage(string p0, int statusCode,
        string? massage = "")
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        ((int)res.StatusCode).Should().BeInRange(400, 499);
        ((int)res.StatusCode).Should().Be(statusCode);
        string result = await res.Content.ReadAsStringAsync();
        result.Should().Contain(massage);

    }

    [Then(@"the ""(.*)"" should receive a failed message with ""(.*)"" status")]
    public void ThenTheShouldReceiveAFailedMessageWithStatus(string p0, int statusCode)
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        ((int)res.StatusCode).Should().BeInRange(400, 499);
        ((int)res.StatusCode).Should().Be(statusCode);
        }

    [When(@"""(.*)"" sends valid credentials on login request")]
    public async Task WhenSendsValidCredentialsOnLoginRequest(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");
        var loginModel = new LoginRequest() { Email = registerModel.Email, Password = registerModel.Password };

        var request = await _httpClient.PostAsJsonAsync("/v1/Account/Login", loginModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [When(@"""(.*)"" open verify email link that sent for '(.*)'")]
    public async Task WhenOpenVerifyEmailLinkThatSentFor(string p0, string p1)
    {
        var confirmationLink = _scenarioContext.Get<string>($"{p0}:EmailConfirmationLink");
        confirmationLink.Should().NotBeNull();

        var request = await _httpClient.GetAsync(new Uri(confirmationLink).PathAndQuery);
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [Then(@"the ""(.*)"" email should be confirmed")]
    public async Task ThenTheEmailShouldBeConfirmed(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");

        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;
        var userManager = scopedServices.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(registerModel.Email);

        user.Should().NotBeNull();
        (await userManager.IsEmailConfirmedAsync(user!)).Should().BeTrue();
    }

    [Then(@"""(.*)"" should receive access token from login")]
    public async Task ThenShouldReceiveAccessTokenFromLogin(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");

        var request = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        request.EnsureSuccessStatusCode();

        var loginResponse = await request.Content.ReadFromJsonAsync<AccessTokenResponse>();

        var client = _applicationFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var infoRequest = new HttpRequestMessage(HttpMethod.Get, "v1/account/manage/info");



        var info = await client.SendAsync(infoRequest);
        info.EnsureSuccessStatusCode();

        var infoResponse = await info.Content.ReadFromJsonAsync<InfoResponse>();

        infoResponse!.Email.Should().BeEquivalentTo(registerModel.Email);
        infoResponse.IsEmailConfirmed.Should().BeTrue();

        _httpClient.DefaultRequestHeaders.Clear();

        _scenarioContext.Set(loginResponse, $"{p0}:{loginResponse.GetType().Name}");
        _scenarioContext.Set(client, $"{p0}:{nameof(HttpClient)}");
        _scenarioContext.Set(loginResponse.AccessToken, $"{p0}:{nameof(loginResponse.AccessToken)}");
    }

    [When(@"""(.*)"" sends invalid credentials on login request")]
    public async Task WhenSendsInvalidCredentialsOnLoginRequest(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");
        var loginModel = new LoginRequest() { Email = registerModel.Email, Password = $"{registerModel.Password}test" };

        var request = await _httpClient.PostAsJsonAsync("/v1/Account/Login", loginModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");    }

    [When(@"""(.*)"" sends forget password request for '(.*)' user")]
    public async Task WhenSendsForgetPasswordRequestForUser(string p0, string p1)
    {
        using var scope = _applicationFactory.Services.CreateScope();

        var forgotPasswordRequest = new ForgotPasswordRequest() { Email = p1};

        var scopedServices = scope.ServiceProvider;
        var emailSenderMock = scopedServices.GetRequiredService<Mock<IEmailSender<User>>>();

        emailSenderMock.Should().NotBeNull();

        string resetPassCode = null;
        string userEmail = null;

        emailSenderMock
            .Setup(e => e.SendPasswordResetCodeAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<User, string, string>((_, email, code) =>
            {
                resetPassCode = code;
                userEmail = email;
            }).Returns(Task.CompletedTask);

        var request = await _httpClient.PostAsJsonAsync("/v1/Account/ForgotPassword", forgotPasswordRequest);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(resetPassCode, $"{p0}:ResetPassCode");
        _scenarioContext.Set(userEmail, $"{p0}:ResetPassUserEmail");
    }

    [Then(@"'(.*)' should receive email contains code for resting user pass")]
    public void ThenShouldReceiveEmailContainsCodeForRestingUserPass(string p0)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");
        var resetPassCode = _scenarioContext.Get<string>($"{p0}:ResetPassCode");
        var userEmail = _scenarioContext.Get<string>($"{p0}:ResetPassUserEmail");

        resetPassCode.Should().NotBeNull();
        userEmail.Should().Be(registerModel.Email);

    }

    [When(@"""(.*)"" open link for resting user pass and send request with code and email and new password ""(.*)""")]
    public async Task WhenOpenLinkForRestingUserPassAndSendRequestWithCodeAndEmailAndNewPassword(string p0, string p1)
    {
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");
        var resetPassCode = _scenarioContext.Get<string>($"{p0}:ResetPassCode");
        var userEmail = _scenarioContext.Get<string>($"{p0}:ResetPassUserEmail");

        resetPassCode.Should().NotBeNull();
        userEmail.Should().Be(registerModel.Email);

        var resetPasswordRequest = new ResetPasswordRequest()
        {
            ResetCode = resetPassCode, NewPassword = p1, Email = userEmail,
        };
        var request = await _httpClient.PostAsJsonAsync("/v1/Account/ResetPassword", resetPasswordRequest);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(resetPasswordRequest, $"{p0}:{resetPasswordRequest.GetType().Name}");


    }

    [When(@"""(.*)"" sends rested credentials on login request")]
    public async Task WhenSendsRestedCredentialsOnLoginRequest(string p0)
    {
        var resetPasswordViewModel = _scenarioContext.Get<ResetPasswordRequest>($"{p0}:{nameof(ResetPasswordRequest)}");
        var loginModel = new LoginRequest() { Email = resetPasswordViewModel.Email, Password = resetPasswordViewModel.NewPassword };

        var request = await _httpClient.PostAsJsonAsync("/v1/Account/Login", loginModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");    }


}
