// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace RamzPardakht.WebApi.Common;

/// <summary>Provides URL validation.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HttpUrlAttribute : DataTypeAttribute
{
    private static IStringLocalizer localizer;

    /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.DataAnnotations.UrlAttribute" /> class.</summary>
    public HttpUrlAttribute()
        : base(DataType.Url)
    {
        ErrorMessage = "Url_Invalid";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;
        if (!(value is string str))
            return new ValidationResult(GetErrorMessage(validationContext));

        if (string.IsNullOrEmpty(str))
            return ValidationResult.Success;

        if (Uri.TryCreate(str, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(GetErrorMessage(validationContext));
    }

    private string GetErrorMessage(ValidationContext validationContext)
    {
        return GetLocalizer(validationContext)[ErrorMessage];
    }

    private IStringLocalizer GetLocalizer(ValidationContext validationContext)
    {
        if (localizer is null)
        {
            var factory = validationContext.GetRequiredService<IStringLocalizerFactory>();
            var annotationOptions =
                validationContext.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
            localizer = annotationOptions.Value.DataAnnotationLocalizerProvider(validationContext.ObjectType, factory);
        }

        return localizer;
    }
}

