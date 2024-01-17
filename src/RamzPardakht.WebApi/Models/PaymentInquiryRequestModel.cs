// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace RamzPardakht.WebApi.Models;

public class PaymentInquiryRequestModel
{
    [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
    public int RefId { get; set; }
}
