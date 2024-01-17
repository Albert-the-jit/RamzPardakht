using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;
using TechTalk.SpecFlow.Assist;

namespace RamzPardakht.Specs.Steps;

[Binding]
public class Payment
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CustomWebApplicationFactory _applicationFactory;

    public Payment(ScenarioContext scenarioContext, CustomWebApplicationFactory applicationFactory)
    {
        _scenarioContext = scenarioContext;
        _applicationFactory = applicationFactory;
    }

    [When(@"""(.*)"" use ""(.*)"" access token and send a create payment request with the following details:")]
    public async Task WhenUseAccessTokenAndSendACreatePaymentRequestWithTheFollowingDetails(string p0, string p1, Table table)
    {
        var client = _scenarioContext.Get<HttpClient>($"Token:{p1}:{nameof(HttpClient)}");

        var paymentCreationRequestModel = table.CreateInstance<PaymentCreationRequestModel>();

        var request = await client.PostAsJsonAsync("/v1/Payment", paymentCreationRequestModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(paymentCreationRequestModel, $"{p0}:{nameof(PaymentCreationRequestModel)}");
    }

    [Then(@"the ""(.*)"" response body should contain the created payment RefId and RedirectUrl and details")]
    public async Task ThenTheResponseBodyShouldContainTheCreatedPaymentRefIdAndRedirectUrlAndDetails(string p0)
    {
        var paymentCreationRequestModel = _scenarioContext.Get<PaymentCreationRequestModel>($"{p0}:{nameof(PaymentCreationRequestModel)}");

        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        var result = await res.Content.ReadFromJsonAsync<PaymentCreationResponseModel>();

        result!.RedirectUrl.Should().NotBeNullOrWhiteSpace();
        result.RefId.Should().BeGreaterThan(0);
        result.ClientRefId.Should().BeEquivalentTo(paymentCreationRequestModel.ClientRefId);
        result.ExpireOn.Should().BeCloseTo(DateTimeOffset.Now.AddMinutes(10), TimeSpan.FromMinutes(5));

        _scenarioContext.Set(result, $"{p0}:{nameof(PaymentCreationResponseModel)}");
    }

    [When(@"""(.*)"" use ""(.*)"" access token and inquiry the ""(.*)"" created payment info")]
    public async Task WhenUseAccessTokenAndInquiryTheCreatedPaymentInfo(string p0, string p1, string p2)
    {
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p2}:{nameof(PaymentCreationResponseModel)}");
        var client = _scenarioContext.Get<HttpClient>($"Token:{p1}:{nameof(HttpClient)}");

        var request = await client.PostAsJsonAsync("/v1/Payment/Inquiry", new PaymentInquiryRequestModel
        {
            RefId = paymentCreationResponseModel.RefId
        });

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");

    }

    [Then(@"the ""(.*)"" response body should contain the created payment RefId with ""(.*)"" Status and details")]
    public async Task ThenTheResponseBodyShouldContainTheCreatedPaymentRefIdWithStatusAndDetails(string p0, Status @new)
    {
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p0}:{nameof(PaymentCreationResponseModel)}");

        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");

        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        var result = await res.Content.ReadFromJsonAsync<PaymentInquiryResponseModel>(jsonSerializerOptions);

        result!.Status.Should().Be(@new);
        result.StatusCode.Should().Be((int)@new);

        result.Currency.Should().Be(Currency.NotSelected);
        result.RefId.Should().Be(paymentCreationResponseModel.RefId);
        result.ClientRefId.Should().BeEquivalentTo(paymentCreationResponseModel.ClientRefId);

        result.PaidAmount.Should().Be(0);
        result.SelectedCurrencyAmount.Should().Be(0);

        result.ExpireOn.Should().BeCloseTo(DateTimeOffset.Now.AddMinutes(10), TimeSpan.FromMinutes(5));

        _scenarioContext.Set(result, $"{p0}:{nameof(PaymentInquiryResponseModel)}");
    }
}
