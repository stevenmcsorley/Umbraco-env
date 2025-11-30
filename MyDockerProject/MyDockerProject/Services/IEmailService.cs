namespace MyDockerProject.Services;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(string toEmail, string toName, string bookingReference, decimal totalPrice, string currency, DateTime checkIn, DateTime? checkOut);
    Task SendBookingCancellationAsync(string toEmail, string toName, string bookingReference);
}

