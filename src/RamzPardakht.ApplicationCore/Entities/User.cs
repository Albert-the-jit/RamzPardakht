// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;

namespace RamzPardakht.ApplicationCore.Entities;

public class User : IdentityUser<int>
{
    public List<UserRole> Roles { get; set; } = new();

}
