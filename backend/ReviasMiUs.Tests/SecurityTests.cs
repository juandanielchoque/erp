using ReviasMiUs.Domain.Settings;
using ReviasMiUs.Domain.Users;
using ReviasMiUs.Domain.Common;
using ReviasMiUs.Infrastructure.Security;

namespace ReviasMiUs.Tests;

public sealed class SecurityTests
{
    [Fact]
    public void PasswordHasher_UsesSaltAndVerifiesWithoutStoringPlainText()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var first = hasher.Hash("Secure123!");
        var second = hasher.Hash("Secure123!");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("Secure123!", first);
        Assert.True(hasher.Verify("Secure123!", first));
        Assert.False(hasher.Verify("Wrong123!", first));
    }

    [Fact]
    public void TokenService_IssuesValidAccessAndRevocableRefreshTokens()
    {
        var service = new InMemoryTokenService();
        var user = new UserAccount("Admin", "admin@test.local", UserRole.Administrator, "hash");

        var issued = service.Issue(user);

        Assert.Equal(user.Id, service.ValidateAccessToken(issued.AccessToken)?.UserId);
        Assert.Equal(user.Id, service.ValidateRefreshToken(issued.RefreshToken));
        service.RevokeAccessToken(issued.AccessToken);
        service.RevokeRefreshToken(issued.RefreshToken);
        Assert.Null(service.ValidateAccessToken(issued.AccessToken));
        Assert.Null(service.ValidateRefreshToken(issued.RefreshToken));
    }

    [Fact]
    public void Settings_RejectInvalidBusinessRuc()
    {
        var settings = new SystemSettings();
        Assert.Throws<DomainException>(() => settings.Update("Empresa", "123", "PEN", 18m, "America/Lima", "B001", "F001", true));
    }

    [Fact]
    public void Settings_ConfiguresThermalReceiptAndRejectsOversizedLogo()
    {
        var settings = new SystemSettings();
        settings.ConfigureReceipt(null, 58, "Left", "Compact", "Hola", "Gracias", true, true, true, true, true, false);

        Assert.Equal(58, settings.ReceiptPaperWidthMm);
        Assert.Equal("Left", settings.ReceiptAlignment);
        Assert.Throws<DomainException>(() => settings.ConfigureReceipt($"data:image/png;base64,{new string('a', 500_000)}", 80, "Center", "Normal", "", "", true, true, true, true, true, true));
    }
}
