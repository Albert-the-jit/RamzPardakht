using System.Net;
using System.Security.Claims;
using Asp.Versioning;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.Resources;
using RamzPardakht.WebApi.Common;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.WebApi.Controllers;

[ApiController]
[Route("v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
[ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.OK)]
[Authorize("WebClient")]
public class AccessTokenController : ControllerBase
{

    private readonly BearerTokenOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IProjectDbContext _projectDbContext;
    private readonly Mapper _mapper;
    private readonly IStringLocalizer<SharedResource> _stringLocalizer;

    public AccessTokenController(
        IOptionsMonitor<BearerTokenOptions> bearerTokenOptionsMonitor,
        TimeProvider timeProvider,
        IProjectDbContext projectDbContext,
        Mapper mapper,
        IStringLocalizer<SharedResource> stringLocalizer
        )
    {
        _timeProvider = timeProvider;
        _projectDbContext = projectDbContext;
        _mapper = mapper;
        _stringLocalizer = stringLocalizer;
        _options = bearerTokenOptionsMonitor.Get(IdentityConstants.BearerScheme);
    }

    /// <summary>
    /// create api access token
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<ReferenceTokenModel>> Post(ReferenceTokenModel model,CancellationToken cancellationToken)
    {
        if (model.ExpiresUtc <= _timeProvider.GetUtcNow())
        {
            ModelState.AddModelError<ReferenceTokenModel>(tokenModel => tokenModel.ExpiresUtc,_stringLocalizer["ShouldBeBiggerThanNow"]);
            return ValidationProblem();
        }

        model.Id = Guid.NewGuid();

       AuthenticationProperties properties = new();

        properties.ExpiresUtc = model.ExpiresUtc;
        var user = new ClaimsIdentity(User.Identity);
        user.AddClaim(new Claim(SystemConst.IsReferenceTokenClaimName,true.ToString()));
        user.AddClaim(new Claim(SystemConst.TokenIdClaimName,model.Id.ToString()));
        user.AddClaim(new Claim(SystemConst.ExpiresUtcClaimName,model.ExpiresUtc.ToString()));


        string token = _options.BearerTokenProtector.Protect(new(new ClaimsPrincipal(user), properties,
            $"{IdentityConstants.BearerScheme}:AccessToken"));

        ReferenceToken referenceToken = _mapper.ToEntity(model);
        referenceToken.UserId = User.GetUserId();
        await _projectDbContext.ReferenceTokens.AddAsync(referenceToken, cancellationToken);
        await _projectDbContext.SaveChangesAsync(cancellationToken);

        var res = _mapper.ToModel(referenceToken);
        res.AccessToken = token;
        return res;

    }

    /// <summary>
    /// list api access tokens
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<Paging<ReferenceTokenModel>>> List([FromQuery] GridifyQuery query,CancellationToken cancellationToken)
    {
        if (!query.IsValid<ReferenceTokenModel>())
        {
            ModelState.AddModelError("", _stringLocalizer["InvalidQuery"]);
            return ValidationProblem(ModelState);
        }
        int userId = User.GetUserId();

        var paging = await _projectDbContext.ReferenceTokens
            .Where(x => x.UserId == userId)
            .ProjectToModel()
            .GridifyAsync(query, cancellationToken);

        return paging;

    }

    /// <summary>
    /// delete api access tokens
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var referenceToken = await _projectDbContext.ReferenceTokens.FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (referenceToken is null)
            return NotFound();

        _projectDbContext.ReferenceTokens.Remove(referenceToken);
        await _projectDbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }
}
