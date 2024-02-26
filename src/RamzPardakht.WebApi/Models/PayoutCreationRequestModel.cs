// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using RamzPardakht.ApplicationCore.Entities;

namespace RamzPardakht.WebApi.Models;

public class PayoutCreationRequestModel
{
    public Currency Currency { get; set; }

    [Range(0.00001, (double)decimal.MaxValue, ErrorMessage = "RangeAttribute_ValidationError")]
    public decimal Amount { get; set; }
    public string ToAddress { get; set; }

}
