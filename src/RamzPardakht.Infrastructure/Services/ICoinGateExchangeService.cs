// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Refit;

namespace RamzPardakht.Infrastructure.Services;

public interface ICoinGateExchangeService
{
    [Get("/rates/merchant/{from}/{to}")]
    Task<IApiResponse<decimal>> GetExchangeRate([AliasAs("from")] string from, [AliasAs("to")] string to);
}
