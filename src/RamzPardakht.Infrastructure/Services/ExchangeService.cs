// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.Infrastructure.Services;

public class ExchangeService : IExchangeService
{
    private readonly ICoinGateExchangeService _coinGateExchangeService;

    public ExchangeService(ICoinGateExchangeService coinGateExchangeService)
    {
        _coinGateExchangeService = coinGateExchangeService;
    }

    public async Task<decimal> ConvertUsdTo(Currency to, decimal usdValue)
    {
        var rate = await _coinGateExchangeService.GetExchangeRate("USD", to.ToString());
        if (rate.IsSuccessStatusCode)
        {
            return usdValue * rate.Content;
        }

        return 0;
    }
}
