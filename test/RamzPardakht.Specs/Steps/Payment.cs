using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Argon;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBXplorer;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.Services;
using RamzPardakht.Infrastructure.Services;
using RamzPardakht.WebApi.Hubs;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;
using Refit;
using RichardSzalay.MockHttp;
using TechTalk.SpecFlow.Assist;
using TypedSignalR.Client;
using AssetMoney = NBitcoin.OpenAsset.AssetMoney;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings;

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

    [When(@"Unauthorized user ""(.*)"" send request to get initial info of ""(.*)"" payment")]
    public async Task WhenUnauthorizedUserSendRequestToGetInitialInfoOfPayment(string p0, string p1)
    {
        using var scope = _applicationFactory.Services.CreateScope();


        var scopedServices = scope.ServiceProvider;
        var coinGateMock = scopedServices.GetRequiredService<Mock<ICoinGateExchangeService>>();

        coinGateMock
            .Setup(e => e.GetExchangeRate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
                new ApiResponse<decimal>(new HttpResponseMessage(HttpStatusCode.OK), 100, new RefitSettings()));

        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        var client = _applicationFactory.CreateClient();

        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var result = await client.GetFromJsonAsync<InitialPaymentInfoForPayerModel>(
            $"v1.0/Payment/InitialInfo/{paymentCreationResponseModel.Code}", jsonSerializerOptions);

        _scenarioContext.Set(result, $"{p0}:{nameof(InitialPaymentInfoForPayerModel)}");

    }

    [Then(
        @"the ""(.*)"" response body of ""(.*)"" payment should contain ""(.*)"" access token name and ""(.*)"" as currency and valid currency amount conversion")]
    public void ThenTheResponseBodyOfPaymentShouldContainAccessTokenNameAndAsCurrencyAndValidCurrencyAmountConversion(
        string p0, string p1, string p2, Currency currency)
    {
        var initialPaymentInfoForPayerModel =
            _scenarioContext.Get<InitialPaymentInfoForPayerModel>($"{p0}:{nameof(InitialPaymentInfoForPayerModel)}");
        var referenceTokenModel = _scenarioContext.Get<ReferenceTokenModel>($"{p1}:{p2}:{nameof(ReferenceTokenModel)}");
        var paymentCreationRequestModel = _scenarioContext.Get<PaymentCreationRequestModel>($"{p1}:{nameof(PaymentCreationRequestModel)}");


        initialPaymentInfoForPayerModel.TokenName.Should().Be(referenceTokenModel.Name);
        initialPaymentInfoForPayerModel.Currency.Should().Be(Currency.NotSelected);
        initialPaymentInfoForPayerModel.CurrenciesAmount.Where(x => x.Currency == Currency.BTC).Should().NotBeNull();
        initialPaymentInfoForPayerModel.CurrenciesAmount.FirstOrDefault(x => x.Currency == Currency.BTC)!.Amount.Should().Be(paymentCreationRequestModel.UsdAmount * 100);
    }

    [When(@"Unauthorized user ""(.*)"" send request to select info of ""(.*)"" payment with the following details:")]
    public async Task WhenUnauthorizedUserSendRequestToSelectInfoOfPaymentWithTheFollowingDetails(string p0, string p1,
        Table table)
    {
        var payerSelectPaymentCurrencyRequestModel = table.CreateInstance<PayerSelectPaymentCurrencyRequestModel>();
        var paymentCreationResponseModel =
            _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        payerSelectPaymentCurrencyRequestModel.Code = paymentCreationResponseModel.Code;

        var client = _applicationFactory.CreateClient();

        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var request = await client.PostAsJsonAsync($"v1.0/Payment/SelectCurrency",
            payerSelectPaymentCurrencyRequestModel, jsonSerializerOptions);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(payerSelectPaymentCurrencyRequestModel,
            $"{p0}:{nameof(PayerSelectPaymentCurrencyRequestModel)}");
    }

    [When(@"Unauthorized user ""(.*)"" send request to get final info of ""(.*)"" payment")]
    public async Task WhenUnauthorizedUserSendRequestToGetFinalInfoOfPayment(string p0, string p1)
    {
        using var scope = _applicationFactory.Services.CreateScope();


        var scopedServices = scope.ServiceProvider;
        var coinGateMock = scopedServices.GetRequiredService<Mock<ICoinGateExchangeService>>();

        coinGateMock
            .Setup(e => e.GetExchangeRate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
                new ApiResponse<decimal>(new HttpResponseMessage(HttpStatusCode.OK), 100, new RefitSettings()));

        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        var client = _applicationFactory.CreateClient();

        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var result = await client.GetFromJsonAsync<PaymentInfoForPayerModel>(
            $"v1.0/Payment/{paymentCreationResponseModel.Code}", jsonSerializerOptions);

        _scenarioContext.Set(result, $"{p0}:{nameof(PaymentInfoForPayerModel)}");
    }

    [Then(@"the ""(.*)"" response body of ""(.*)"" payment should contain ""(.*)"" currency and valid address and valid amount and ""(.*)"" payed amount and ""(.*)"" status")]
    public void ThenTheResponseBodyOfPaymentShouldContainCurrencyAndValidAddressAndValidAmountAndPayedAmountAndStatus(string p0, string p1, Currency currency, decimal payedAmount, Status status)
    {
        var paymentInfoForPayerModel =
            _scenarioContext.Get<PaymentInfoForPayerModel>($"{p0}:{nameof(PaymentInfoForPayerModel)}");
        var paymentCreationRequestModel = _scenarioContext.Get<PaymentCreationRequestModel>($"{p1}:{nameof(PaymentCreationRequestModel)}");
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");


        paymentInfoForPayerModel.Currency.Should().Be(currency);
        paymentInfoForPayerModel.Amount.Should().Be(paymentCreationRequestModel.UsdAmount * 100);
        paymentInfoForPayerModel.SuccessUrl.Should().Be(paymentCreationRequestModel.SuccessUrl);
        paymentInfoForPayerModel.ClientRefId.Should().Be(paymentCreationRequestModel.ClientRefId);
        paymentInfoForPayerModel.RefId.Should().Be(paymentCreationResponseModel.RefId);
        paymentInfoForPayerModel.Status.Should().Be(status);
        paymentInfoForPayerModel.PaidAmount.Should().Be(payedAmount);
        try
        {
            BitcoinAddress.Create(paymentInfoForPayerModel.Address, Network.TestNet);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [When(@"Unauthorized user ""(.*)"" connect and listen for ""(.*)"" payment notification")]
    public async Task WhenUnauthorizedUserConnectAndListenForPaymentNotification(string p0, string p1)
    {
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/Hub/PaymentHub", o =>
            {
                o.Transports = HttpTransportType.WebSockets;
                o.SkipNegotiation = true;

                o.WebSocketFactory = (context, cancellationToken) =>
                {
                    var webSocketClient = _applicationFactory.Server.CreateWebSocketClient();
                    var webSocketTask = webSocketClient.ConnectAsync(context.Uri, cancellationToken);
                    return new (webSocketTask);
                };
            })
            .Build();
        IPaymentHub hubProxy = connection.CreateHubProxy<IPaymentHub>();
        connection.KeepAliveInterval = TimeSpan.FromSeconds(10);
        connection.Reconnected += async (s) =>
        {
            await connection.StartAsync();
            await hubProxy.ListenToPaymentByCode(new ListenToPaymentByCodeMessageModel()
            {
                Code = paymentCreationResponseModel.Code
            });
        };
        await connection.StartAsync();
        await hubProxy.ListenToPaymentByCode(new ListenToPaymentByCodeMessageModel()
        {
            Code = paymentCreationResponseModel.Code
        });

        var clientMock = new Mock<IPaymentClient>();
        clientMock
            .Setup(client => client.TransactionPartiallyPayed(It.IsAny<TransactionPayedMessageModel>()))
            .Callback<TransactionPayedMessageModel>(
                model =>
                {
                    _scenarioContext.Set(model,$"{p0}:{nameof(IPaymentClient.TransactionPartiallyPayed)}");
                });

        clientMock
            .Setup(client => client.TransactionFullyPayed(It.IsAny<TransactionPayedMessageModel>()))
            .Callback<TransactionPayedMessageModel>(
                model =>
                {
                    _scenarioContext.Set(model,$"{p0}:{nameof(IPaymentClient.TransactionFullyPayed)}");
                });


        connection.Register(clientMock.Object);

        _scenarioContext.Set(connection,$"{p0}:SignalR");
    }

    [When(@"Unauthorized user ""(.*)"" has been broadcast transaction to ""(.*)"" payment address in ""(.*)"" blockchain with ""(.*)"" confirmation and ""(.*)"" as payment amount")]
    public async Task WhenUnauthorizedUserHasBeenBroadcastTransactionToPaymentAddressInBlockchainWithConfirmationAndAsPaymentAmount(string p0, string p1, Currency currency, int confirmation, decimal amount)
    {
        decimal payedAmount = 0;
        _scenarioContext.TryGetValue($"{p0}:PayedAmount", out payedAmount);
        payedAmount += amount;


        var network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var paymentInfoForPayerModel =
            _scenarioContext.Get<PaymentInfoForPayerModel>($"{p0}:{nameof(PaymentInfoForPayerModel)}");
        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;
        var mockHttpMessageHandler = scopedServices.GetRequiredService<MockHttpMessageHandler>();
        var bitcoinWalletProvider = scopedServices.GetRequiredService<IBitcoinWalletProvider>();

        var userDerivationScheme =
            network.DerivationStrategyFactory.CreateDirectDerivationStrategy(bitcoinWalletProvider.GetMasterPublicKey());

        var transactionEvent = new NewTransactionEvent()
        {
            EventId = 1,
            DerivationStrategy = userDerivationScheme,
            TransactionData = new TransactionResult() { Confirmations = confirmation, },
            Outputs = new List<MatchedOutput>()
            {
                new MatchedInput()
                {
                    Address = BitcoinAddress.Create(paymentInfoForPayerModel.Address, Network.TestNet),
                    Value = Money.FromUnit(amount, MoneyUnit.BTC),
                }
            }
        };
        string json = transactionEvent.ToJObject(new Serializer(network).Settings).ToString();
        mockHttpMessageHandler.ResetBackendDefinitions();



        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/events?limit=30&longPolling=True")
            .Respond("application/json", $"[{json}]");
        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/events?limit=30&longPolling=True&lastEventId=*")
            .Respond("application/json", $"[]");

        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/addresses/*/balance")
            .Respond("application/json", network.Serializer.ToString(new GetBalanceResponse
            {
                Unconfirmed = Money.Zero,
                Available = Money.Zero,
                Confirmed = new Money(payedAmount, MoneyUnit.BTC),
                Immature = Money.Zero,
                Total = new Money(payedAmount, MoneyUnit.BTC),
            }));
        _scenarioContext.Set(transactionEvent,$"{p0}:{nameof(NewTransactionEvent)}");

        _scenarioContext.Set(payedAmount,$"{p0}:PayedAmount");
        await Task.Delay(2000);

    }

    [Then(@"Unauthorized user ""(.*)"" should receive notification for partially paid payment of ""(.*)""")]
    public void ThenUnauthorizedUserShouldReceiveNotificationForPartiallyPaidPaymentOf(string p0, string p1)
    {
        var transactionPartiallyPayed =
            _scenarioContext.Get<TransactionPayedMessageModel>($"{p0}:{nameof(IPaymentClient.TransactionPartiallyPayed)}");
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        transactionPartiallyPayed.Code.Should().Be(paymentCreationResponseModel.Code);
    }

    [Then(@"Unauthorized user ""(.*)"" should receive notification for fully paid payment of ""(.*)""")]
    public void ThenUnauthorizedUserShouldReceiveNotificationForFullyPaidPaymentOf(string p0, string p1)
    {
        var transactionFullyPayed =
            _scenarioContext.Get<TransactionPayedMessageModel>($"{p0}:{nameof(IPaymentClient.TransactionFullyPayed)}");
        var paymentCreationResponseModel = _scenarioContext.Get<PaymentCreationResponseModel>($"{p1}:{nameof(PaymentCreationResponseModel)}");

        transactionFullyPayed.Code.Should().Be(paymentCreationResponseModel.Code);
    }
}
