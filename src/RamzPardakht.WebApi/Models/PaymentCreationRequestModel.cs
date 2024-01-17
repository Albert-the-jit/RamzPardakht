// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using RamzPardakht.WebApi.Common;

namespace RamzPardakht.WebApi.Models;

public class PaymentCreationRequestModel
{
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Only_Positive_Number_Allowed")]
    public decimal UsdAmount { get; set; }
    [HttpUrl]
    [StringLength(200,ErrorMessage = "StringLengthAttribute_ValidationError")]
    [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
    public string CallbackUrl { get; set; }
    [HttpUrl]
    [StringLength(200,ErrorMessage = "StringLengthAttribute_ValidationError")]
    public string? WebhookUrl { get; set; }
    [StringLength(50,ErrorMessage = "StringLengthAttribute_ValidationError")]
    public string? ClientRefId { get; set; }
    [StringLength(200,ErrorMessage = "StringLengthAttribute_ValidationError")]
    public string? Description { get; set; }

}
