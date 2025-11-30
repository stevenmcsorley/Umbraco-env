using System.Net;
using System.Net.Mail;
using System.Text;

namespace MyDockerProject.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendBookingConfirmationAsync(
        string toEmail, 
        string toName, 
        string bookingReference, 
        decimal totalPrice, 
        string currency, 
        DateTime checkIn, 
        DateTime? checkOut)
    {
        try
        {
            var subject = $"Booking Confirmation - {bookingReference}";
            var body = GenerateBookingConfirmationEmail(toName, bookingReference, totalPrice, currency, checkIn, checkOut);

            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Booking confirmation email sent to {Email} for booking {Reference}", toEmail, bookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation email to {Email}", toEmail);
            // Don't throw - email failures shouldn't break booking creation
        }
    }

    public async Task SendBookingCancellationAsync(string toEmail, string toName, string bookingReference)
    {
        try
        {
            var subject = $"Booking Cancelled - {bookingReference}";
            var body = GenerateBookingCancellationEmail(toName, bookingReference);

            await SendEmailAsync(toEmail, toName, subject, body);
            _logger.LogInformation("Booking cancellation email sent to {Email} for booking {Reference}", toEmail, bookingReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking cancellation email to {Email}", toEmail);
            // Don't throw - email failures shouldn't break cancellation
        }
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
    {
        // Check if email is enabled
        var emailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);
        if (!emailEnabled)
        {
            _logger.LogInformation("Email sending is disabled. Would send to {Email}: {Subject}", toEmail, subject);
            _logger.LogInformation("Email body:\n{Body}", body);
            return;
        }

        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var smtpUsername = _configuration["Email:SmtpUsername"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@example.com";
        var fromName = _configuration["Email:FromName"] ?? "Booking System";

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP host not configured. Email not sent to {Email}", toEmail);
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort);
        client.EnableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);
        
        if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
        {
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
        }

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(toEmail, toName));

        await client.SendMailAsync(message);
    }

    private string GenerateBookingConfirmationEmail(string name, string bookingReference, decimal totalPrice, string currency, DateTime checkIn, DateTime? checkOut)
    {
        var checkOutText = checkOut.HasValue ? checkOut.Value.ToString("dd MMM yyyy") : "N/A";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .booking-details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Booking Confirmed!</h1>
        </div>
        <div class=""content"">
            <p>Dear {name},</p>
            <p>Thank you for your booking. Your reservation has been confirmed.</p>
            <div class=""booking-details"">
                <h3>Booking Details</h3>
                <p><strong>Booking Reference:</strong> {bookingReference}</p>
                <p><strong>Check-in:</strong> {checkIn:dd MMM yyyy}</p>
                <p><strong>Check-out:</strong> {checkOutText}</p>
                <p><strong>Total Amount:</strong> {currency} {totalPrice:F2}</p>
            </div>
            <p>Please keep this booking reference for your records. You will receive a reminder email closer to your check-in date.</p>
            <p>If you have any questions, please contact us.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateBookingCancellationEmail(string name, string bookingReference)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .booking-details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #f44336; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Booking Cancelled</h1>
        </div>
        <div class=""content"">
            <p>Dear {name},</p>
            <p>Your booking has been cancelled as requested.</p>
            <div class=""booking-details"">
                <h3>Booking Reference</h3>
                <p><strong>{bookingReference}</strong></p>
            </div>
            <p>If you have any questions or if this cancellation was made in error, please contact us immediately.</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}

