// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RamzPardakht.WebApi.Common;

public class HideSignalRHubSpecificationFromSwaggerFilter : IDocumentFilter
{

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Remove(swaggerDoc.Paths.FirstOrDefault(x => x.Key.Contains("signalr-dev/spec.json")).Key);
    }
}
