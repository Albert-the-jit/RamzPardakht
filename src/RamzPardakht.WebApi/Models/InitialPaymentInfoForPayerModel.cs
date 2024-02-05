// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class InitialPaymentInfoForPayerModel
{
    public string TokenName { get; set; }
    public decimal UsdAmount { get; set; }
    public List<CurrencyAmount> CurrenciesAmount { get; set; } = new();

    public Currency Currency { get; set; }
    public string? PayerEmail { get; set; }
    public DateTimeOffset ExpireOn { get; set; }

}

public class CurrencyAmount
{
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}
