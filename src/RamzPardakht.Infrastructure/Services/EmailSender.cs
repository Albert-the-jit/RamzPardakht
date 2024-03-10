// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RamzPardakht.ApplicationCore.Common;
using RamzPardakht.ApplicationCore.Entities;
using Resend;

namespace RamzPardakht.Infrastructure.Services;

public class EmailSender : IEmailSender<User>
{
    private readonly IResend _resend;
    private readonly IOptionsSnapshot<AppSettings> _optionsSnapshot;

    public EmailSender(IResend resend,
        IOptionsSnapshot<AppSettings> optionsSnapshot
        )
    {
        _resend = resend;
        _optionsSnapshot = optionsSnapshot;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
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
        string urlDecode = HttpUtility.UrlDecode(confirmationLink);
        var uriBuilder = new UriBuilder(urlDecode);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        uriBuilder = new UriBuilder(_optionsSnapshot.Value.ConfirmedEmailFrontAddress) { Query = query.ToString() };

        return SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(uriBuilder.ToString())}'>clicking here</a>.");
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink) => throw new NotImplementedException();


    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {

        var uriBuilder = new UriBuilder(_optionsSnapshot.Value.ResetPasswordFrontAddress);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query["Email"] = email;
        query["ResetCode"] = resetCode;

        uriBuilder.Query = query.ToString();

        return SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(uriBuilder.ToString())}'>clicking here</a>.");

    }
}
