// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.ApplicationCore.Contracts;

public interface IExchangeService
{
    Task<decimal> ConvertUsdTo(Currency to, decimal usdValue);
}
