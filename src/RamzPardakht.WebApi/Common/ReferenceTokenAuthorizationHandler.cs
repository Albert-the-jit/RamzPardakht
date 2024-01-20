// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Contracts;

namespace RamzPardakht.WebApi.Common;

public class ReferenceTokenAuthorizationHandler : AuthorizationHandler<ReferenceTokenRequirement>
{
    private readonly IProjectDbContext _projectDbContext;
    private readonly TimeProvider _timeProvider;

    public ReferenceTokenAuthorizationHandler(IProjectDbContext projectDbContext, TimeProvider timeProvider)
    {
        _projectDbContext = projectDbContext;
        _timeProvider = timeProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        ReferenceTokenRequirement requirement)
    {
        if (context.User.Claims.Any(claim =>
                claim.Type == SystemConst.IsReferenceTokenClaimName && claim.Value == true.ToString()))
        {
            int userId = context.User.GetUserId();
            string tokenId = context.User.Claims.FirstOrDefault(x => x.Type == SystemConst.TokenIdClaimName)!.Value;
            DateTimeOffset expiresUtc =
                DateTimeOffset.Parse(context.User.Claims.FirstOrDefault(x => x.Type == SystemConst.ExpiresUtcClaimName)!
                    .Value);
            var referenceToken = await _projectDbContext.ReferenceTokens.FindAsync(new Guid(tokenId));

            if (referenceToken is not null && _timeProvider.GetUtcNow() <= expiresUtc)
            {
                if (referenceToken.UserId == userId &&
                    (expiresUtc - referenceToken.ExpiresUtc) <= TimeSpan.FromMinutes(5))
                {
                    context.User.Identities.FirstOrDefault()
                        ?.AddClaim(new Claim(SystemConst.ReferenceTokenNameClaimName, referenceToken.Name));

                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }
        }
        else
        {
            context.Succeed(requirement);
        }
    }
}
