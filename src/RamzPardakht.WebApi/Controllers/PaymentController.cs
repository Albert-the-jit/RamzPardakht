using System.Net;
using Asp.Versioning;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NBitcoin;
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
public class PaymentController : ControllerBase
{

    private readonly TimeProvider _timeProvider;
    private readonly IProjectDbContext _projectDbContext;
    private readonly Mapper _mapper;
    private readonly IExchangeService _exchangeService;
    private readonly IBitcoinWalletProvider _bitcoinWalletProvider;
    private readonly IStringLocalizer<SharedResource> _stringLocalizer;

    private readonly ExplorerClient _explorerClient;
    private readonly NBXplorerNetwork _network;
    private readonly IConfiguration _configuration;

    public PaymentController(
        TimeProvider timeProvider,
        IProjectDbContext projectDbContext,
        Mapper mapper,
        IHttpClientFactory httpClientFactory,
        IExchangeService exchangeService,
        IBitcoinWalletProvider bitcoinWalletProvider,
        IStringLocalizer<SharedResource> stringLocalizer,
        IConfiguration configuration
        )
    {
        _timeProvider = timeProvider;
        _projectDbContext = projectDbContext;
        _mapper = mapper;
        _exchangeService = exchangeService;
        _bitcoinWalletProvider = bitcoinWalletProvider;
        _stringLocalizer = stringLocalizer;
        _configuration = configuration;


        _network = new NBXplorerNetworkProvider(ChainName.Testnet).GetBTC();

        var httpClient = httpClientFactory.CreateClient(nameof(ExplorerClient));
        ExplorerClient client = new ExplorerClient(_network, new Uri(configuration["NBXplorer:Endpoint"]!));
        client.SetClient(httpClient);

        _explorerClient = client;
    }


    /// <summary>
    /// report of payments
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<Paging<PaymentReportModel>>> List([FromQuery] GridifyQuery query,
        CancellationToken cancellationToken)
    {

        if (!query.IsValid<PaymentReportModel>())
        {
            ModelState.AddModelError("", _stringLocalizer["InvalidQuery"]);
            return ValidationProblem(ModelState);
        }

        var payments = await _projectDbContext.Payments
            .Where(x => x.UserId == User.GetUserId())
            .ProjectToModel()
            .GridifyAsync(query, cancellationToken);

        return payments;
    }

    /// <summary>
    /// create payment and return payment page url
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<PaymentCreationResponseModel>> Post(PaymentCreationRequestModel model,
        CancellationToken cancellationToken)
    {
        var payment = _mapper.ToEntity(model);
        payment.ExpireOn = _timeProvider.GetUtcNow().AddMinutes(25);
        payment.UserId = User.GetUserId();

        if (payment.Currency != Currency.NotSelected)
        {
            decimal amount = await _exchangeService.ConvertUsdTo(payment.Currency, payment.UsdAmount);
            payment.Amount = amount;
        }

        await _projectDbContext.Payments.AddAsync(payment, cancellationToken);
        await _projectDbContext.SaveChangesAsync(cancellationToken);

        return new PaymentCreationResponseModel()
        {
            ClientRefId = payment.ClientRefId,
            RedirectUrl = $"https://example.com/payment/{payment.Code}",
            Code = payment.Code,
            RefId = payment.Id,
            ExpireOn = payment.ExpireOn,
        };
    }

    /// <summary>
    /// inquiry payment and return payment info and status
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("Inquiry")]
    public async Task<ActionResult<PaymentInquiryResponseModel>> Inquiry(PaymentInquiryRequestModel model,
        CancellationToken cancellationToken)
    {
        var payment =
            await _projectDbContext.Payments.FirstOrDefaultAsync(
                x => x.Id == model.RefId && x.UserId == User.GetUserId(), cancellationToken);
        if (payment is null)
            return NotFound();

        var result = _mapper.ToModel(payment);
        result.RefId = payment.Id;
        result.SelectedCurrencyAmount = payment.Amount;

        return result;
    }

    /// <summary>
    /// inquiry initial payment info and return payment info and currencies amount
    /// </summary>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("InitialInfo/{code:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<InitialPaymentInfoForPayerModel>> GetInitialInfo(Guid code,
        CancellationToken cancellationToken)
    {
        var payment = await _projectDbContext.Payments
            .Include(x => x.CreatedByToken)
            .FirstOrDefaultAsync(x => x.Code == code && x.Status == Status.New, cancellationToken);
        if (payment is null)
            return NotFound();

        var info = new InitialPaymentInfoForPayerModel()
        {
            TokenName = payment?.CreatedByToken?.Name ?? "",
            LogoId = payment?.CreatedByToken?.LogoId,
            UsdAmount = payment!.UsdAmount,
        };

        foreach (Currency currency in Enum.GetValues<Currency>().Where(x => x != Currency.NotSelected))
        {
            decimal amount = await _exchangeService.ConvertUsdTo(currency, payment.UsdAmount);
            if (amount != decimal.Zero)
            {
                info.CurrenciesAmount.Add(new CurrencyAmount() { Amount = amount, Currency = currency });
            }
        }

        info.Currency = payment.Currency;
        info.PayerEmail = payment.PayerEmail;
        info.ExpireOn = payment.ExpireOn;

        return info;
    }

    /// <summary>
    /// select payment currency
    /// </summary>
    /// <param name="model"></param>
    /// <param name="_"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("SelectCurrency")]
    [AllowAnonymous]
    public async Task<ActionResult> SelectCurrency(
        PayerSelectPaymentCurrencyRequestModel model,
        [FromHeader(Name = "X-XSRF-TOKEN")] string? _,
        CancellationToken cancellationToken)
    {
        var payment = await _projectDbContext.Payments
            .FirstOrDefaultAsync(x => x.Code == model.Code && x.Currency == Currency.NotSelected && x.Status == Status.New, cancellationToken);
        if (payment is null)
            return NotFound();

        payment.Currency = model.Currency;
        payment.PayerEmail = model.PayerEmail;

        decimal amount = await _exchangeService.ConvertUsdTo(payment.Currency, payment.UsdAmount);
        payment.Amount = amount;

        await _projectDbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    /// <summary>
    /// get finalize payment status and payment founds status
    /// </summary>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{code:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentInfoForPayerModel>> Get(Guid code,
        CancellationToken cancellationToken)
    {
        var payment = await _projectDbContext.Payments.Include(x => x.Wallet)
            .FirstOrDefaultAsync(x => x.Code == code && x.Currency != Currency.NotSelected && (x.Status != Status.Paid), cancellationToken);

        if (payment is null)
            return NotFound();

        if (payment.Currency == Currency.BTC)
        {
            if (payment?.Wallet is null)
            {
                var wallet = new Wallet() { Address = "", Currency = payment.Currency };
                payment.Wallet = wallet;

                await _projectDbContext.SaveChangesAsync(CancellationToken.None);
                (WalletVersion walletVersion, PubKey pubKey) = _bitcoinWalletProvider.GetNewWalletPublicKey(wallet!.Id);

                wallet.Address = pubKey.GetAddress(ScriptPubKeyType.Segwit, Network.TestNet).ToString();
                wallet.Version = walletVersion;
                wallet.Path = wallet.Id;

                await _projectDbContext.SaveChangesAsync(CancellationToken.None);

                var addressTrackedSource =
                    new AddressTrackedSource(BitcoinAddress.Create(payment.Wallet.Address, _network.NBitcoinNetwork));

                await _explorerClient.TrackAsync(addressTrackedSource, cancellation: cancellationToken);

                await _projectDbContext.SaveChangesAsync(CancellationToken.None);

                return new PaymentInfoForPayerModel
                {
                    RefId = payment.Id,
                    ClientRefId = payment.ClientRefId,
                    Currency = payment.Currency,
                    Address = payment.Wallet.Address,
                    Amount = payment.Amount,
                    PaidAmount = 0,
                    SuccessUrl =
                        $"{Request.Scheme}://{Request.Host.ToUriComponent()}/v1/Payment/Redirect/{payment.Code}",
                    Status = payment.Status,
                };
            }
            return new PaymentInfoForPayerModel
            {
                RefId = payment.Id,
                ClientRefId = payment.ClientRefId,
                Currency = payment.Currency,
                Address = payment.Wallet.Address,
                Amount = payment.Amount,
                SuccessUrl = $"{Request.Scheme}://{Request.Host.ToUriComponent()}/v1/Payment/Redirect/{payment.Code}",
                PaidAmount = payment.PaidAmount,
                Status = payment.Status,
            };
        }
        else
        {
            return ValidationProblem($"{payment.Currency} Not Implemented");
        }
    }

    [HttpGet("Redirect/{code:guid}")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult> Redirect(Guid code, [FromQuery] bool isSuccess = true,
        CancellationToken cancellationToken = default)
    {

        var payment = await _projectDbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (payment is null)
            return NotFound();

        if (isSuccess == true &&
            payment.Status == Status.Pending &&
            payment.PaidAmount >= payment.Amount &&
            payment.ExpireOn >= _timeProvider.GetUtcNow())

            return Redirect(payment.SuccessUrl);

        if (isSuccess == false &&
            !string.IsNullOrEmpty(payment.CancelUrl) &&
            payment.ExpireOn >= _timeProvider.GetUtcNow().AddMinutes(-5))

            return Redirect(payment.CancelUrl);

        return NotFound();

    }
}
