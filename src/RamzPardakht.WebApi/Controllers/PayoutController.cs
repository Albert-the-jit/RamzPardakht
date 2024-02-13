using System.Net;
using Asp.Versioning;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NBitcoin;
using NBitcoin.RPC;
using NBXplorer;
using NBXplorer.Models;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.Resources;
using RamzPardakht.WebApi.Common;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.WebApi.Controllers;

[ApiController]
[Route("v{version:apiVersion}/[controller]")]
[ApiVersion(1)]
[Produces("application/json")]
[Consumes("application/json")]
[ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.OK)]
[Authorize]
public class PayoutController : ControllerBase
{
    private readonly IProjectDbContext _projectDbContext;
    private readonly Mapper _mapper;
    private readonly IStringLocalizer<SharedResource> _stringLocalizer;

    private readonly ExplorerClient _explorerClient;
    private readonly IBitcoinWalletProvider _bitcoinWalletProvider;

    public PayoutController(
        IProjectDbContext projectDbContext,
        Mapper mapper,
        IHttpClientFactory httpClientFactory,
        IBitcoinWalletProvider bitcoinWalletProvider, IStringLocalizer<SharedResource> stringLocalizer)
    {
        _projectDbContext = projectDbContext;
        _mapper = mapper;
        _bitcoinWalletProvider = bitcoinWalletProvider;
        _stringLocalizer = stringLocalizer;

        NBXplorerNetwork network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var httpClient = httpClientFactory.CreateClient(nameof(ExplorerClient));
        ExplorerClient client = new ExplorerClient(network, new Uri("http://localhost:32838"));
        client.SetClient(httpClient);

        _explorerClient = client;

    }

    /// <summary>
    /// report of payouts
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<Paging<PayoutReportModel>>> List([FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {
        if (!query.IsValid<PayoutReportModel>())
        {
            ModelState.AddModelError("", _stringLocalizer["InvalidQuery"]);
            return ValidationProblem(ModelState);
        }

        var payouts = await _projectDbContext.Payouts
            .ProjectToModel()
            .GridifyAsync(query, cancellationToken);

        return payouts;
    }

    /// <summary>
    /// create payout
    /// </summary>
    /// <param name="model">dont fill id param</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<PayoutCreationResponseModel>> Post(PayoutCreationRequestModel model)
    {
        try
        {
            BitcoinAddress.Create(model.ToAddress, Network.TestNet);
        }
        catch (Exception e)
        {
            ModelState.AddModelError<PayoutCreationRequestModel>(requestModel => requestModel.ToAddress, _stringLocalizer["InvalidWalletAddress"]);
            return ValidationProblem();
        }

        var partiallySettled = await _projectDbContext.Payments
            .Include(x => x.Wallet)
            .Include(x => x.PaymentPayouts)
            .ThenInclude(x => x.Payout)
            .Where(x => x.UserId == User.GetUserId())
            .Where(payment => payment.Currency == model.Currency)
            .Where(payment => payment.Status == Status.Paid)
            .Where(payment => payment.PaymentPayouts.Count != 0)
            .Where(payment =>
                payment.PaymentPayouts
                    .Where(x => x.Payout.Status != PayoutStatus.Failed)
                    .Sum(payoutPayment => payoutPayment.Amount) < payment.Amount
            ).ToListAsync();

        var payout = new Payout()
        {
            Currency = model.Currency,
            Amount = model.Amount,
            Status = PayoutStatus.Unconfirmed,
            ToAddress = model.ToAddress,
            UserId = User.GetUserId(),

        };
        foreach (Payment payment in partiallySettled)
        {
            decimal remainingPayoutAmount = payout.Amount - payout.PayoutPayments.Sum(x => x.Amount);

            if (remainingPayoutAmount > 0)
            {

                decimal paymentPayoutSum = payment.PaymentPayouts.Where(x => x.Payout.Status != PayoutStatus.Failed)
                    .Sum(payoutPayment => payoutPayment.Amount);

                decimal paymentPayoutAmount = payment.Amount - paymentPayoutSum;

                if (paymentPayoutAmount > remainingPayoutAmount)
                    paymentPayoutAmount = remainingPayoutAmount;


                var paymentPayout = new PayoutPayment()
                {
                    Payout = payout,
                    Payment = payment,
                    Amount = paymentPayoutAmount,
                    PaymentId = payment.Id,

                };
                payout.PayoutPayments.Add(paymentPayout);
            }
            else
            {
                break;
            }

        }

        decimal remainingPayoutAmountAfterPartiallySettled = payout.Amount - payout.PayoutPayments.Sum(x => x.Amount);

        if (remainingPayoutAmountAfterPartiallySettled > 0)
        {
            var notSettled = await _projectDbContext.Payments
                .Include(x => x.Wallet)
                .Include(x => x.PaymentPayouts)
                .ThenInclude(x => x.Payout)
                .Where(x => x.UserId == User.GetUserId())
                .Where(payment => payment.Currency == model.Currency)
                .Where(payment => payment.Status == Status.Paid)
                .Where(payment => payment.PaymentPayouts.Count == 0)
                .ToListAsync();

            foreach (Payment payment in notSettled)
            {
                decimal remainingPayoutAmount = payout.Amount - payout.PayoutPayments.Sum(x => x.Amount);

                if (remainingPayoutAmount > 0)
                {

                    decimal paymentPayoutSum = payment.PaymentPayouts.Where(x => x.Payout.Status != PayoutStatus.Failed)
                        .Sum(payoutPayment => payoutPayment.Amount);

                    decimal paymentPayoutAmount = payment.Amount - paymentPayoutSum;

                    if (paymentPayoutAmount > remainingPayoutAmount)
                        paymentPayoutAmount = remainingPayoutAmount;


                    var paymentPayout = new PayoutPayment()
                    {
                        Payout = payout,
                        Payment = payment,
                        Amount = paymentPayoutAmount,
                        PaymentId = payment.Id,

                    };
                    payout.PayoutPayments.Add(paymentPayout);
                }
                else
                {
                    break;
                }

            }
        }

        decimal finalRemainingPayoutAmount = payout.Amount - payout.PayoutPayments.Sum(x => x.Amount);

        if (finalRemainingPayoutAmount == 0)
        {

            while (true)
            {
                BitcoinAddress payoutAddress = BitcoinAddress.Create(payout.ToAddress,
                    Network.TestNet);

                TransactionBuilder builder = Network.TestNet.CreateTransactionBuilder();
                foreach (var payoutPayment in payout.PayoutPayments)
                {
                    BitcoinAddress bitcoinAddress = BitcoinAddress.Create(
                        payoutPayment.Payment.Wallet.Address,
                        Network.TestNet);

                    var addressTrackedSource = new AddressTrackedSource(bitcoinAddress);

                    var utxos = await _explorerClient.GetUTXOsAsync(addressTrackedSource);

                    var coins = utxos.GetUnspentCoins();
                    builder.AddCoins(coins);

                    var key = _bitcoinWalletProvider.GetPrivateKeyById(payoutPayment.Payment.Wallet.Version,
                        payoutPayment.Payment.Wallet.Id);

                    builder.AddKeys(key);

                    decimal payoutPaymentAmountChange =
                        coins.Sum(coin => coin.Amount.ToDecimal(MoneyUnit.BTC)) - payoutPayment.Amount;

                    builder.Send(bitcoinAddress,
                        Money.FromUnit(payoutPaymentAmountChange, MoneyUnit.BTC));

                }

                builder.SetChange(payoutAddress);

                // Set the fee rate

                var fallbackFeeRate = new FeeRate(Money.Satoshis(100), 1);
                var feeRate = (await _explorerClient.GetFeeRateAsync(1, fallbackFeeRate)).FeeRate;

                builder.SendEstimatedFees(feeRate);

                var tx = builder.BuildTransaction(true);

                var result = await _explorerClient.BroadcastAsync(tx);
                if (result.Success)
                {
                    payout.Status = PayoutStatus.Unconfirmed;
                    payout.TransactionId = tx.GetHash().ToString();
                    payout.NetworkFee = feeRate.GetFee(tx).ToDecimal(MoneyUnit.BTC);

                    await _projectDbContext.Payouts.AddAsync(payout);
                    await _projectDbContext.SaveChangesAsync();

                    return _mapper.ToModel(payout);

                }
                else if (result.RPCCode.HasValue && result.RPCCode.Value == RPCErrorCode.RPC_TRANSACTION_REJECTED)
                {
                    Console.WriteLine("We probably got a conflict, let's try again!");
                }
                else
                {
                    Console.WriteLine(
                        $"Something is really wrong {result.RPCCode} {result.RPCCodeMessage} {result.RPCMessage}");
                    return Problem("Something is really wrong");
                    // Do something!!!
                }
            }

        }
        else if (finalRemainingPayoutAmount < 0)
        {
            return Problem("Something is really wrong");
        }
        else if (finalRemainingPayoutAmount > 0)
        {
            ModelState.AddModelError<PayoutCreationRequestModel>(requestModel => requestModel.Amount, _stringLocalizer["NotEnoughMoney"]);
            return ValidationProblem();
        }
        else
        {
            ModelState.AddModelError<PayoutCreationRequestModel>(requestModel => requestModel.Amount, _stringLocalizer["NotEnoughMoney"]);
            return ValidationProblem();
        }
    }
}

