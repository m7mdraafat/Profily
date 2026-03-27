namespace Profily.Core.Interfaces;

public interface ITokenEncryptionService
{
    byte[] Encrypt(string plainText);
    string Decrypt(byte[] cipherText);
}