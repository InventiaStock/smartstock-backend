using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartStock.Platform.API.Shared.Contracts;

namespace SmartStock.Platform.API.AlertsRestocking;

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

public class SmtpEmailSender(
    IOptions<SmtpSettings> options) : IEmailSender
{
    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody)
    {
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            settings.User,
            settings.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string From { get; set; } = "whatsapp:+14155238886";
}

public class TwilioWhatsAppSender(
    System.Net.Http.HttpClient http,
    IOptions<TwilioSettings> options) : IWhatsAppSender
{
    public async Task SendAsync(string toPhone, string text)
    {
        var settings = options.Value;
        var phone = toPhone.Replace(" ", string.Empty);

        if (string.IsNullOrEmpty(phone)) return;

        using var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{settings.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes(
                $"{settings.AccountSid}:{settings.AuthToken}"));

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", credentials);

        request.Content = new System.Net.Http.FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["From"] = settings.From,
                ["To"] = $"whatsapp:{phone}",
                ["Body"] = text
            });

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}