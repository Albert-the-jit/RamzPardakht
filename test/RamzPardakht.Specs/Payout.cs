using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;
using RichardSzalay.MockHttp;
using TechTalk.SpecFlow.Assist;

namespace RamzPardakht.Specs;

[Binding]
public class Payout
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CustomWebApplicationFactory _applicationFactory;

    public Payout(ScenarioContext scenarioContext, CustomWebApplicationFactory applicationFactory)
    {
        _scenarioContext = scenarioContext;
        _applicationFactory = applicationFactory;
    }

    [When(@"""(.*)"" sends payout request with the following information:")]
    public async Task WhenSendsPayoutRequestWithTheFollowingInformation(string p0, Table table)
    {
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");
        var registerModel = _scenarioContext.Get<RegisterRequest>($"{p0}:{nameof(RegisterRequest)}");

        var payoutCreationRequestModel = table.CreateInstance<PayoutCreationRequestModel>();


        var network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;
        var mockHttpMessageHandlers = scopedServices.GetRequiredService<Dictionary<string, MockHttpMessageHandler>>();
        var mockHttpMessageHandler = mockHttpMessageHandlers.TryGet(nameof(ExplorerClient));
        var bitcoinWalletProvider = scopedServices.GetRequiredService<IBitcoinWalletProvider>();
        var projectDbContext = scopedServices.GetRequiredService<IProjectDbContext>();

        var user = await projectDbContext.Users.FirstOrDefaultAsync(x => x.Email == registerModel.Email);

        var userPayments = await projectDbContext.Payments.Where(x => x.UserId == user.Id && x.Status == Status.Paid).ToListAsync();

        ;

        DerivationStrategyBase directDerivationStrategy =
            network.DerivationStrategyFactory.CreateDirectDerivationStrategy(bitcoinWalletProvider.GetMasterPublicKey());

        KeyPath keyPath = new KeyPath("1");

        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/addresses/*/utxos")
            .Respond("application/json",
                network.Serializer.ToString(new UTXOChanges
                {
                    Unconfirmed = new UTXOChange(),
                    DerivationStrategy =
                        directDerivationStrategy,
                    Confirmed = new UTXOChange()
                    {
                        UTXOs = new List<UTXO>()
                        {
                            new UTXO()
                            {
                                Value = new Money(userPayments.Sum(x => x.PaidAmount) / 2, MoneyUnit.BTC),
                                KeyPath = keyPath,
                                Confirmations = 6,
                                Outpoint = new OutPoint(uint256.Zero, 0),
                                ScriptPubKey = directDerivationStrategy.GetDerivation(keyPath).ScriptPubKey
                            },
                            new UTXO()
                            {
                                Value = new Money(userPayments.Sum(x => x.PaidAmount) / 2, MoneyUnit.BTC),
                                KeyPath = keyPath,
                                Confirmations = 6,
                                Outpoint = new OutPoint(uint256.One, 1),
                                ScriptPubKey = directDerivationStrategy.GetDerivation(keyPath).ScriptPubKey
                            },
                        }
                    }
                }));

        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/fees/*")
            .Respond("application/json", network.Serializer.ToString(new GetFeeRateResult { FeeRate = new FeeRate(10M), BlockCount = 1 }));

        mockHttpMessageHandler.When($"http://*/v1/cryptos/*/transactions").WithQueryString("testMempoolAccept=False")
            .Respond("application/json", network.Serializer.ToString(new BroadcastResult { Success = true }));



        var request = await client.PostAsJsonAsync("/v1/Payout", payoutCreationRequestModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(payoutCreationRequestModel, $"{p0}:{nameof(PayoutCreationRequestModel)}");

        mockHttpMessageHandler.ResetBackendDefinitions();

    }

    [Then(@"the ""(.*)"" response body should contain the created payout and details")]
    public async Task ThenTheResponseBodyShouldContainTheCreatedPayoutAndDetails(string p0)
    {
        var payoutCreationRequestModel = _scenarioContext.Get<PayoutCreationRequestModel>($"{p0}:{nameof(PayoutCreationRequestModel)}");
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");

        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        var paymentCreationResponseModel = await res.Content.ReadFromJsonAsync<PayoutCreationResponseModel>(jsonSerializerOptions);


        paymentCreationResponseModel.Should().NotBeNull();

        paymentCreationResponseModel!.Currency.Should().Be(payoutCreationRequestModel.Currency);
        paymentCreationResponseModel.Amount.Should().Be(payoutCreationRequestModel.Amount);
        paymentCreationResponseModel.ToAddress.Should().Be(payoutCreationRequestModel.ToAddress);
        paymentCreationResponseModel.NetworkFee.Should().BeGreaterThan(0);
        paymentCreationResponseModel.TransactionId.Should().NotBeNullOrWhiteSpace();
    }
}
