using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Gridify;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;
using TechTalk.SpecFlow.Assist;

namespace RamzPardakht.Specs.Steps;

[Binding]
public class AccessToken
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CustomWebApplicationFactory _applicationFactory;

    public AccessToken(ScenarioContext scenarioContext, CustomWebApplicationFactory applicationFactory)
    {
        _applicationFactory = applicationFactory;
        _scenarioContext = scenarioContext;
    }
    [When(@"""(.*)"" send a create access token request with random date within previous month as ExpiresUtc and the following details:")]
    public async Task WhenSendACreateAccessTokenRequestWithRandomDateWithinPreviousMonthAsExpiresUtcAndTheFollowingDetails(string p0, Table table)
    {
        int randomDay = new Random().Next(3, 20);
        var expiresUtc = DateTimeOffset.Now.AddMonths(-1).AddDays(randomDay);

        var referenceTokenModel = table.CreateInstance<ReferenceTokenModel>();
        referenceTokenModel.ExpiresUtc = expiresUtc;
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");

        var request = await client.PostAsJsonAsync("/v1/AccessToken", referenceTokenModel);
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(referenceTokenModel, $"{p0}:{nameof(ReferenceTokenModel)}");
    }

    [When(
        @"""(.*)"" send a create access token request with random date within next month as ExpiresUtc and the following details:")]
    public async Task WhenSendACreateAccessTokenRequestWithRandomDateWithinNextMonthAsExpiresUtcAndTheFollowingDetails(
        string p0, Table table)
    {
        int randomDay = new Random().Next(3, 20);
        var expiresUtc = DateTimeOffset.Now.AddMonths(1).AddDays(randomDay);

        var referenceTokenModel = table.CreateInstance<ReferenceTokenModel>();
        referenceTokenModel.ExpiresUtc = expiresUtc;
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");

        var request = await client.PostAsJsonAsync("/v1/AccessToken", referenceTokenModel);
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(referenceTokenModel, $"{p0}:{nameof(ReferenceTokenModel)}");
    }

    [Then(@"the ""(.*)"" response body should contain the created access token and item unique identifier and details")]
    public async Task ThenTheResponseBodyShouldContainTheCreatedAccessTokenAndItemUniqueIdentifierAndDetails(string p0)
    {
        var referenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p0}:{nameof(ReferenceTokenModel)}");

        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        var result = await res.Content.ReadFromJsonAsync<ReferenceTokenModel>();

        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Id.Should().NotBeEmpty();

        result.Should().BeEquivalentTo(referenceTokenModel, options => options.Excluding(z => z.Id)
            .Excluding(o => o.AccessToken));

        _scenarioContext.Set(result, $"{p0}:{result.Name}:{nameof(ReferenceTokenModel)}");
    }



    [When(@"""(.*)"" use ""(.*)"" access token on account info")]
    public async Task WhenUseAccessTokenOnAccountInfo(string p0, string p1)
    {
        var referenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p0}:{p1}:{nameof(ReferenceTokenModel)}");

        var client = _applicationFactory.CreateClient();


        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", referenceTokenModel.AccessToken);

        var infoRequest = new HttpRequestMessage(HttpMethod.Get, "v1/account/manage/info");
        var request = await client.SendAsync(infoRequest);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [Then(@"the ""(.*)"" response body should contain correct user info")]
    public async Task ThenTheResponseBodyShouldContainCorrectUserInfo(string p0)
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        var infoResponse = await res.Content.ReadFromJsonAsync<InfoResponse>();
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");

        infoResponse.Should().NotBeNull();
        infoResponse!.Email.Should().Be(registerModel.Email);
    }

    [When(@"""(.*)"" use ""(.*)"" access token and send a create access token request with random date within next month as ExpiresUtc and the following details:")]
    public async Task WhenUseAccessTokenAndSendACreateAccessTokenRequestWithRandomDateWithinNextMonthAsExpiresUtcAndTheFollowingDetails(string p0, string p1, Table table)
    {
        int randomDay = new Random().Next(3, 20);
        var expiresUtc = DateTimeOffset.Now.AddMonths(1).AddDays(randomDay);
        var createdReferenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p0}:{p1}:{nameof(ReferenceTokenModel)}");
        var client = _applicationFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", createdReferenceTokenModel.AccessToken);

        var referenceTokenModel = table.CreateInstance<ReferenceTokenModel>();
        referenceTokenModel.ExpiresUtc = expiresUtc;


        var request = await client.PostAsJsonAsync("/v1/AccessToken", referenceTokenModel);
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(referenceTokenModel, $"{p0}:{nameof(ReferenceTokenModel)}");
    }

    [When(@"""(.*)"" send request to list created access token details expire within next month")]
    public async Task WhenSendRequestToListCreatedAccessTokenDetailsExpireWithinNextMonth(string p0)
    {
        var startExpiresUtc = new DateTime(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month + 1, 1);
        var endExpiresUtc = new DateTime(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month + 1, 28);

        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");

        var request = await client.GetFromJsonAsync<Paging<ReferenceTokenModel>>($"/v1/AccessToken?PageSize=100&filter=ExpiresUtc>{startExpiresUtc},ExpiresUtc<{endExpiresUtc}");
        _scenarioContext.Set(request, $"{p0}:{request.GetType().FullName}");

    }

    [Then(@"""(.*)"" should receive response contain the ""(.*)"" access token")]
    public void ThenShouldReceiveResponseContainTheAccessToken(string p0, string p1)
    {
        var createdReferenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p0}:{p1}:{nameof(ReferenceTokenModel)}");
        var referenceTokenList = _scenarioContext.Get<Paging<ReferenceTokenModel>>($"{p0}:{typeof(Paging<ReferenceTokenModel>).FullName}");

        referenceTokenList.Count.Should().BeGreaterThan(0);
        referenceTokenList.Data.FirstOrDefault(x => x.Name == p1).Should().BeEquivalentTo(createdReferenceTokenModel,
            options => options.Excluding(o => o.AccessToken)
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1))).WhenTypeIs<DateTimeOffset>());
    }

    [When(@"""(.*)"" send request remove ""(.*)"" access token")]
    public async Task WhenSendRequestRemoveAccessToken(string p0, string p1)
    {
        var referenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p0}:{p1}:{nameof(ReferenceTokenModel)}");
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");
        var request = await client.DeleteAsync($"/v1/AccessToken/{referenceTokenModel.Id}");
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");    }
}
