// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class UserBalanceInfoResponse
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}
