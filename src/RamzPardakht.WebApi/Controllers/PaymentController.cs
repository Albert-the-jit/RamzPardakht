using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Contracts;
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

    public PaymentController(
        TimeProvider timeProvider,
        IProjectDbContext projectDbContext,
        Mapper mapper
    )
    {
        _timeProvider = timeProvider;
        _projectDbContext = projectDbContext;
        _mapper = mapper;
    }

    /// <summary>
    /// create payment and return payment page url
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<PaymentCreationResponseModel>> Post(PaymentCreationRequestModel model,
        CancellationToken cancellationToken)
    {
        var payment = _mapper.ToEntity(model);
        payment.ExpireOn = _timeProvider.GetUtcNow().AddMinutes(15);
        payment.UserId = User.GetUserId();

        await _projectDbContext.Payments.AddAsync(payment, cancellationToken);
        await _projectDbContext.SaveChangesAsync(cancellationToken);

        return new PaymentCreationResponseModel()
        {
            ClientRefId = payment.ClientRefId,
            RedirectUrl = $"https://example.com/payment/{payment.Code}",
            RefId = payment.Id,
            ExpireOn = payment.ExpireOn,
        };
    }

    /// <summary>
    /// inquiry payment and return payment info and status
    /// </summary>
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
}
