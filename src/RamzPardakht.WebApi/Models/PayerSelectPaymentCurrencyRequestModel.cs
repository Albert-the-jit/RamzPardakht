// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class PayerSelectPaymentCurrencyRequestModel
{
    [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
    public Guid Code { get; set; }

    [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
    [AllowedValues(Currency.BTC, ErrorMessage = "AllowedValuesAttribute_Invalid")]
    public Currency Currency { get; set; }

    [EmailAddress(ErrorMessage = "EmailAddressAttribute_Invalid")]
    public string? PayerEmail { get; set; }
}
