using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
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
public class BalanceController : ControllerBase
{
    private readonly IProjectDbContext _projectDbContext;


    public BalanceController(
        IProjectDbContext projectDbContext
        )
    {
        _projectDbContext = projectDbContext;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<List<UserBalanceInfoResponse>>> Get()
    {
        var result = new List<UserBalanceInfoResponse>();
        foreach (Currency currency in Enum.GetValues<Currency>().Where(x => x != Currency.NotSelected))
        {
            decimal @in = await _projectDbContext.Payments
                .Where(x => x.Currency == currency)
                .Where(x => x.UserId == User.GetUserId())
                .Where(x => x.Status == Status.Paid).SumAsync(x => x.Amount);

            decimal @out = await _projectDbContext.Payouts
                .Where(x => x.Currency == currency)
                .Where(x => x.UserId == User.GetUserId())
                .Where(x => x.Status != PayoutStatus.Failed)
                .SumAsync(x => x.Amount);

            result.Add(new UserBalanceInfoResponse()
            {
                Amount = @in - @out,
                Currency = currency
            });
        }

        return result;
    }
}

