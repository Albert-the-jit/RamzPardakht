using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Gridify;
using MassTransit.Testing;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NBitcoin;
using NBXplorer;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.MessageModels;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;
using TechTalk.SpecFlow.Assist;

namespace RamzPardakht.Specs.Steps;

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
        var bitcoinWalletProvider = scopedServices.GetRequiredService<IBitcoinWalletProvider>();
        var projectDbContext = scopedServices.GetRequiredService<IProjectDbContext>();

        var explorerClientAdapterMock = scopedServices.GetRequiredService<Mock<IExplorerClientAdapter<Bitcoin>>>();

        var user = await projectDbContext.Users.FirstOrDefaultAsync(x => x.Email == registerModel.Email);

        var userPayments = await projectDbContext.Payments.Where(x => x.UserId == user.Id && x.Status == Status.Paid).ToListAsync();

        DerivationStrategyBase directDerivationStrategy =
            network.DerivationStrategyFactory.CreateDirectDerivationStrategy(bitcoinWalletProvider.GetMasterPublicKey());

        KeyPath keyPath = new KeyPath("1");

        explorerClientAdapterMock.Setup(adapter => adapter.GetUTXOsAsync(It.IsAny<TrackedSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UTXOChanges
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
            });

        explorerClientAdapterMock.Setup(adapter => adapter.GetFeeRateAsync(It.IsAny<int>(), It.IsAny<FeeRate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetFeeRateResult { FeeRate = new FeeRate(10M), BlockCount = 1 });

        explorerClientAdapterMock.Setup(adapter => adapter.BroadcastAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BroadcastResult { Success = true });

        var request = await client.PostAsJsonAsync("/v1/Payout", payoutCreationRequestModel);

        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
        _scenarioContext.Set(payoutCreationRequestModel, $"{p0}:{nameof(PayoutCreationRequestModel)}");

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

        _scenarioContext.Set(paymentCreationResponseModel, $"{p0}:{nameof(PayoutCreationResponseModel)}");

    }

    [Then(@"the ""(.*)"" payout transaction should broadcast")]
    public void ThenThePayoutTransactionShouldBroadcast(string p0)
    {
        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;

        var explorerClientAdapterMock = scopedServices.GetRequiredService<Mock<IExplorerClientAdapter<Bitcoin>>>();

        explorerClientAdapterMock.Verify();
    }

    [When(@"after user ""(.*)"" payout request broadcast and confirmed for ""(.*)"" time")]
    public async Task WhenAfterUserPayoutRequestBroadcastAndConfirmedForTime(string p0, int confirmationCount)
    {
        var network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();


        var harness = _applicationFactory.Services.GetRequiredService<ITestHarness>();

        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;

        var explorerClientAdapterMock = scopedServices.GetRequiredService<Mock<IExplorerClientAdapter<Bitcoin>>>();

        explorerClientAdapterMock.Setup(adapter =>
                adapter.GetTransactionAsync(It.IsAny<uint256>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionResult { Confirmations = confirmationCount, });

        await harness.Bus.Publish(new NewBitcoinBlockEvent());

        await Task.Delay(2000);

        explorerClientAdapterMock.Verify();
    }

    [When(@"""(.*)"" send list request for payouts")]
    public async Task WhenSendListRequestForPayouts(string p0)
    {
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");
        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        var request = await client.GetFromJsonAsync<Paging<PayoutReportModel>>("/v1/Payout", jsonSerializerOptions);
        _scenarioContext.Set(request, $"{p0}:{request!.GetType().FullName}");

    }

    [Then(@"the ""(.*)"" response body should contain the created payout with transaction id and network fee and ""(.*)"" status")]
    public void ThenTheResponseBodyShouldContainTheCreatedPayoutWithTransactionIdAndNetworkFeeAndStatus(string p0, PayoutStatus status)
    {
        var payoutCreationResponseModel = _scenarioContext.Get<PayoutCreationResponseModel>($"{p0}:{nameof(PayoutCreationResponseModel)}");

        var payouts = _scenarioContext.Get<Paging<PayoutReportModel>>($"{p0}:{typeof(Paging<PayoutReportModel>).FullName}");

        payouts.Should().NotBeNull();

        payouts.Data.Should().Contain(model => model.Id == payoutCreationResponseModel.Id);

        payouts.Data.First(x => x.Id == payoutCreationResponseModel.Id).Should()
            .BeEquivalentTo(payoutCreationResponseModel, options => options.ExcludingMissingMembers());

        payouts.Data.First(x => x.Id == payoutCreationResponseModel.Id).TransactionId.Should().NotBeNullOrEmpty();

        payouts.Data.First(x => x.Id == payoutCreationResponseModel.Id).NetworkFee.Should().BeGreaterThan(0);
        payouts.Data.First(x => x.Id == payoutCreationResponseModel.Id).Status.Should().Be(status);
    }

    [When(@"""(.*)"" sends user balance request")]
    public async Task WhenSendsUserBalanceRequest(string p0)
    {
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");
        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        var request = await client.GetFromJsonAsync<List<UserBalanceInfoResponse>>("/v1/Balance", jsonSerializerOptions);
        _scenarioContext.Set(request, $"{p0}:{request!.GetType().FullName}");
    }

    [Then(@"the ""(.*)"" should receive response that hase balance for ""(.*)"" and (.*) as amount")]
    public void ThenTheShouldReceiveResponseThatHaseBalanceForAndAsAmount(string p0, Currency currency, int amount)
    {
        var balanceInfoResponses = _scenarioContext.Get<List<UserBalanceInfoResponse>>($"{p0}:{typeof(List<UserBalanceInfoResponse>).FullName}");

        balanceInfoResponses.Should().ContainSingle(response => response.Currency == currency);

        balanceInfoResponses.First(response => response.Currency == currency).Amount.Should().Be(amount);

    }
}
