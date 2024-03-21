// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Entities;
using Resend;

namespace RamzPardakht.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Only call external services so its easier to mock IEmailSender so this class will have no test coverage")]
public class EmailSender : IEmailSender<User>
{
    private readonly IResend _resend;
    private readonly IOptions<AppSettings> _options;

    public EmailSender(IResend resend,
        IOptions<AppSettings> options
        )
    {
        _resend = resend;
        _options = options;
    }

    private async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new EmailMessage();
        message.From = "email@zaferanii.ir";
        message.To.Add(email);
        message.Subject = subject;
        message.HtmlBody = htmlMessage;

        await _resend.EmailSendAsync(message);
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        string urlDecode = WebUtility.HtmlDecode(confirmationLink);
        var uriBuilder = new UriBuilder(urlDecode);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        uriBuilder = new UriBuilder(_options.Value.ConfirmedEmailFrontAddress) { Query = query.ToString() };

        return SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(uriBuilder.ToString())}'>clicking here</a>.");
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink) => throw new NotImplementedException();


    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {

        var uriBuilder = new UriBuilder(_options.Value.ResetPasswordFrontAddress);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["email"] = email;
        query["resetCode"] = resetCode;

        uriBuilder.Query = query.ToString();

        return SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(uriBuilder.ToString())}'>clicking here</a>.");

    }
}
