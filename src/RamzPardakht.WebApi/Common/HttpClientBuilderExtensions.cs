// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog.HttpClient;

namespace RamzPardakht.WebApi.Common;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Configures the HttpMessageHandler pipeline to collect Prometheus metrics.
    /// </summary>
    public static IServiceCollection UseHttpClientLogRequestResponse(
        this IServiceCollection services,
        Action<RequestLoggingOptions> configureOptions = null)
    {
        if (configureOptions == null)
            configureOptions = (Action<RequestLoggingOptions>)(options => { });
        services.Configure(null, configureOptions);

        services.TryAddTransient<LoggingDelegatingHandler>(s =>
            new LoggingDelegatingHandler(s.GetRequiredService<IOptionsSnapshot<RequestLoggingOptions>>().Get(null),
                forHttpClientFactory: true));

        return services.ConfigureAll<HttpClientFactoryOptions>(optionsToConfigure =>
            optionsToConfigure.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.AdditionalHandlers.Add(
                    builder.Services.GetRequiredService<LoggingDelegatingHandler>())));

    }
}
