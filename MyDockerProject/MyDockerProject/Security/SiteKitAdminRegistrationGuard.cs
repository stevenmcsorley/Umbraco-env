using System.Security.Cryptography;
using System.Text;

namespace MyDockerProject.Security;

public static class SiteKitAdminRegistrationGuard
{
    public static bool CanRegister(string email, string? suppliedCode, IConfiguration configuration)
    {
        var allowlist = (configuration["SiteKit:AdminEmails"] ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowlist.Contains(email.Trim(), StringComparer.OrdinalIgnoreCase)) return true;
        var expected = configuration["SiteKit:AdminRegistrationCode"];
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(suppliedCode) || expected.Length > 512 || suppliedCode.Length > 512) return false;
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedCode);
        var equal = CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
        CryptographicOperations.ZeroMemory(expectedBytes);
        CryptographicOperations.ZeroMemory(suppliedBytes);
        return equal;
    }
}
