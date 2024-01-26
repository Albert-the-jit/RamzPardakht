// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RamzPardakht.WebApi.Models;

public class PaymentCreationResponseModel
{
    public int RefId { get; set; }
    public string RedirectUrl { get; set; }
    public Guid Code { get; set; }
    public string? ClientRefId { get; set; }
    public DateTimeOffset ExpireOn { get; set; }

}
