using SiteKit.Core.Security;

namespace SiteKit.Core.Tests;

public sealed class BookingManagementCredentialTests
{
    [Fact]
    public void ManagementTokenIsUnpredictableAndStoredOnlyAsAHash()
    {
        var token = BookingManagementCredential.CreateToken();
        var otherToken = BookingManagementCredential.CreateToken();

        Assert.Equal(43, token.Length);
        Assert.NotEqual(token, otherToken);
        Assert.Equal(64, BookingManagementCredential.HashToken(token).Length);
        Assert.NotEqual(token, BookingManagementCredential.HashToken(token));
        Assert.NotEqual(BookingManagementCredential.HashToken(token), BookingManagementCredential.HashToken(otherToken));
    }
}
