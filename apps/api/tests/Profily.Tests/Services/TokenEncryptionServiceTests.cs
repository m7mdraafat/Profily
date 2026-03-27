using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Profily.Infrastructure.Services;
using Profily.Infrastructure.Settings;

namespace Profily.Tests.Services;

public sealed class TokenEncryptionServiceTests
{
    private readonly TokenEncryptionService _sut;

    public TokenEncryptionServiceTests()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        var settings = Options.Create(new SecuritySettings
        {
            TokenEncryptionKey = Convert.ToBase64String(key)
        });
        _sut = new TokenEncryptionService(settings);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalText()
    {
        var original = "ghp_abc123_test_token_value";

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        var plainText = "same_token";

        var encrypted1 = _sut.Encrypt(plainText);
        var encrypted2 = _sut.Encrypt(plainText);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        var encrypted = _sut.Encrypt("test_token");
        encrypted[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => _sut.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var encrypted = _sut.Encrypt("test_token");

        var differentKey = new byte[32];
        RandomNumberGenerator.Fill(differentKey);
        var otherService = new TokenEncryptionService(Options.Create(new SecuritySettings
        {
            TokenEncryptionKey = Convert.ToBase64String(differentKey)
        }));

        Assert.ThrowsAny<CryptographicException>(() => otherService.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_WithTooShortCiphertext_ThrowsArgumentException()
    {
        var shortData = new byte[10];

        var ex = Assert.Throws<ArgumentException>(() => _sut.Decrypt(shortData));
        Assert.Contains("too short", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyKey_ThrowsInvalidOperationException()
    {
        var settings = Options.Create(new SecuritySettings { TokenEncryptionKey = "" });

        Assert.Throws<InvalidOperationException>(() => new TokenEncryptionService(settings));
    }

    [Fact]
    public void Constructor_WithWrongKeyLength_ThrowsArgumentException()
    {
        var shortKey = new byte[16];
        var settings = Options.Create(new SecuritySettings
        {
            TokenEncryptionKey = Convert.ToBase64String(shortKey)
        });

        Assert.Throws<ArgumentException>(() => new TokenEncryptionService(settings));
    }

    [Fact]
    public void Encrypt_Decrypt_HandlesUnicodeText()
    {
        var original = "token_with_unicode_chars";

        var encrypted = _sut.Encrypt(original);
        var decrypted = _sut.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }
}
