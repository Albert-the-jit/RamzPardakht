// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class PaymentInquiryResponseModel
{
    public int RefId { get; set; }
    public string? ClientRefId { get; set; }
    public Status Status { get; set; }
    public DateTimeOffset ExpireOn { get; set; }

    public int StatusCode
    {
        get => (int)Status;
    }

    /// <summary>
    /// selected currency
    /// </summary>
    public Currency Currency { get; set; }

    /// <summary>
    /// computed amount in selected currency
    /// </summary>
    public decimal SelectedCurrencyAmount { get; set; }

    /// <summary>
    /// paid amount in selected currency
    /// </summary>
    public decimal PaidAmount { get; set; }

}
