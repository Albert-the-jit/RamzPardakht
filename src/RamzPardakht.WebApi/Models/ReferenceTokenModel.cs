// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace RamzPardakht.WebApi.Models;

public class ReferenceTokenModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
    public string Name  { get; set; }
    public string? Description  { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset ExpiresUtc { get; set; }
    public string? AccessToken { get; set; }

}
