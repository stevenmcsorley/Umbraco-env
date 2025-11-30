using System.Text;

namespace MyDockerProject.Services;

/// <summary>
/// Mock payment service for development/testing.
/// This can be easily swapped out for a real payment provider (Stripe, PayPal, etc.)
/// by implementing IPaymentService with the real provider's SDK.
/// </summary>
public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;

    public MockPaymentService(ILogger<MockPaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        _logger.LogInformation("Processing mock payment: {Amount} {Currency} for {CustomerEmail}", 
            request.Amount, request.Currency, request.CustomerEmail);

        // Simulate payment processing delay
        await Task.Delay(500);

        // Simulate payment success (90% success rate for testing)
        var random = new Random();
        var success = random.NextDouble() > 0.1; // 90% success rate

        if (success)
        {
            var paymentId = $"MOCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            var transactionId = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

            _logger.LogInformation("Mock payment successful: {PaymentId}", paymentId);

            return new PaymentResult
            {
                Success = true,
                PaymentId = paymentId,
                TransactionId = transactionId,
                Metadata = new Dictionary<string, string>
                {
                    { "provider", "mock" },
                    { "method", "mock_card" },
                    { "processed_at", DateTime.UtcNow.ToString("O") }
                }
            };
        }
        else
        {
            _logger.LogWarning("Mock payment failed: Insufficient funds (simulated)");

            return new PaymentResult
            {
                Success = false,
                PaymentId = string.Empty,
                TransactionId = string.Empty,
                ErrorMessage = "Payment declined: Insufficient funds (mock simulation)",
                Metadata = new Dictionary<string, string>
                {
                    { "provider", "mock" },
                    { "error_code", "insufficient_funds" }
                }
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string paymentId, decimal amount)
    {
        _logger.LogInformation("Processing mock refund: {Amount} for payment {PaymentId}", amount, paymentId);

        // Simulate refund processing delay
        await Task.Delay(300);

        var refundId = $"REFUND-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        _logger.LogInformation("Mock refund successful: {RefundId} for payment {PaymentId}", refundId, paymentId);

        return new PaymentResult
        {
            Success = true,
            PaymentId = paymentId,
            TransactionId = refundId,
            Metadata = new Dictionary<string, string>
            {
                { "provider", "mock" },
                { "type", "refund" },
                { "refunded_at", DateTime.UtcNow.ToString("O") }
            }
        };
    }
}

