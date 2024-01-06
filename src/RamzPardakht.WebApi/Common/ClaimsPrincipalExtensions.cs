// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace RamzPardakht.WebApi.Common;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
            throw new ArgumentNullException(nameof(principal));
        string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(id, out int userId))
            return userId;
        return 0;
    }
}
