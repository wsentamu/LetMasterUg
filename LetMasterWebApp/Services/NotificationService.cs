using LetMasterWebApp.Core;
using LetMasterWebApp.DataLayer;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text;
using System.Text.Json;

namespace LetMasterWebApp.Services;
//SMS and email notification service
public interface INotificationService
{
    public Task SendEmailAsync(string toEmail, string subject, string message);
    public Task SendSms(string phoneNumber, string message);
}
public class NotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    public NotificationService(IConfiguration configuration, ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }
    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        _logger.LogInformation($"Sending {subject} to {toEmail}");

        var senderName = _configuration["NotificationSettings:SenderName"];
        var senderEmail = _configuration["NotificationSettings:SenderEmail"];
        var smtpServer = _configuration["NotificationSettings:SmtpServer"];
        var smtpPort = _configuration.GetValue<int>("NotificationSettings:SmtpPort");
        var emailUsername = _configuration["NotificationSettings:Username"];
        var emailPassword = _configuration["NotificationSettings:Password"];

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
        emailMessage.To.Add(new MailboxAddress("", toEmail));
        emailMessage.Subject = subject;
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message
        };
        emailMessage.Body = bodyBuilder.ToMessageBody();

        bool success = false;

        using (var client = new SmtpClient())
        {
            try
            {
                // Optional: you might want to set client.ServerCertificateValidationCallback to validate server certificates
                await client.ConnectAsync(smtpServer, smtpPort, true);
                await client.AuthenticateAsync(emailUsername, emailPassword);
                await client.SendAsync(emailMessage);
                success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending email");
            }
            finally
            {
                await client.DisconnectAsync(true);
                client.Dispose();
            }
        }

        var notification = new UserMessage
        {
            IsActive = success,
            MessageBody = message,
            MessageMode = "EMAIL",
            MessageReciepient = toEmail,
            MessageSubject = subject
        };

        await SaveNotification(notification);
    }
    public async Task SendSms(string phoneNumber, string message)
    {
        //sanitize the phone number, to 256 format the call speeda mobile api
        try
        {
            var mobile = StringHelper.CleanPhone(phoneNumber);
            var smsUrl = _configuration.GetValue<string>("NotificationSettings:SmsUrl");
            var api_id = _configuration.GetValue<string>("NotificationSettings:SmsApiId");
            var api_password = _configuration.GetValue<string>("NotificationSettings:SmsApiPassword");
            var sender_id = _configuration.GetValue<string>("NotificationSettings:SmsSenderId");
            var sms_type = _configuration.GetValue<string>("NotificationSettings:SmsType");
            var userMsg = new UserMessage
            {
                MessageBody = message,
                MessageMode = "SMS",
                MessageReciepient = mobile,
                IsActive = false,
            };
            var request = new SmsRequest
            {
                api_id = api_id!,
                api_password = api_password!,
                encoding = "T",
                sms_type = sms_type!,
                phonenumber = phoneNumber,
                sender_id = sender_id!,
                textmessage = message,
            };
            var client = new HttpClient();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(smsUrl, content);
            var respContent = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
                userMsg.IsActive = true;
            await SaveNotification(userMsg);
        }
        catch (Exception ex)
        {
            // handle exception
            _logger.LogError($"{ex.Message}");
            //throw new InvalidOperationException("Could not send sms", ex);
        }
    }
    private async Task SaveNotification(UserMessage message)
    {
        try
        {
            _logger.LogInformation($"Save Notification {JsonSerializer.Serialize(message)}");
            await _context.UserMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }
        catch
        {
            _logger.LogError($"Failed to save notification: {message}");
        }
    }
}