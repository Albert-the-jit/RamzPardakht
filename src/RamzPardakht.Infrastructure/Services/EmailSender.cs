// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

namespace RamzPardakht.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IResend _resend;

    public EmailSender(IResend resend)
    {
        _resend = resend;
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
}
