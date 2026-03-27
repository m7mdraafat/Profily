using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Settings;

namespace Profily.Infrastructure.Services;

public sealed class TokenEncryptionService : ITokenEncryptionService
{
    private readonly byte[] _key;

    public TokenEncryptionService(IOptions<SecuritySettings> settings)
    {
        var keyBase64 = settings.Value.TokenEncryptionKey;
        if (string.IsNullOrEmpty(keyBase64))
        {
            throw new InvalidOperationException("Security:TokenEncryptionKey is not configured.");
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new ArgumentException("Encryption key must be 32 bytes (256-bit).");
        }
    }

    public byte[] Encrypt(string plainText)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        CryptographicOperations.ZeroMemory(plainBytes);

        // Store as: nonce (12) + tag (16) + ciphertext (variable)
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        cipherBytes.CopyTo(result, nonce.Length + tag.Length);

        return result;
    }

    public string Decrypt(byte[] cipherText)
    {
        var minLength = AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize;
        if (cipherText.Length < minLength)
            throw new ArgumentException($"Cipher text too short. Minimum {minLength} bytes required.", nameof(cipherText));

        var nonce = cipherText[..AesGcm.NonceByteSizes.MaxSize];
        var tag = cipherText[AesGcm.NonceByteSizes.MaxSize..(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)];
        var cipher = cipherText[(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)..];

        var plainBytes = new byte[cipher.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipher, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}