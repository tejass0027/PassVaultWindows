using System.Security.Cryptography;

namespace PassVaultWindows.Crypto;

/// <summary>
/// Generic AES-256-GCM helpers. Every encrypted blob is laid out as
/// [12-byte nonce][ciphertext][16-byte tag] - the same byte layout the Android app's
/// javax.crypto-based implementation produces, so a .pvbk backup exported from either
/// platform decrypts identically on the other.
/// </summary>
public static class CryptoManager
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public static byte[] RandomBytes(int size)
    {
        var bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /// <summary>A fresh random 256-bit key, e.g. for use as the vault's Data Encryption Key.</summary>
    public static byte[] GenerateRandomKey() => RandomBytes(KeySizeBytes);

    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = RandomBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(key, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var result = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes + ciphertext.Length, TagSizeBytes);
        return result;
    }

    /// <summary>Throws <see cref="CryptographicException"/> if <paramref name="key"/> is wrong (tag mismatch).</summary>
    public static byte[] Decrypt(byte[] nonceCiphertextTag, byte[] key)
    {
        if (nonceCiphertextTag.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new ArgumentException("Encrypted blob too short");
        }

        int cipherLength = nonceCiphertextTag.Length - NonceSizeBytes - TagSizeBytes;
        var nonce = new byte[NonceSizeBytes];
        var ciphertext = new byte[cipherLength];
        var tag = new byte[TagSizeBytes];

        Buffer.BlockCopy(nonceCiphertextTag, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(nonceCiphertextTag, NonceSizeBytes, ciphertext, 0, cipherLength);
        Buffer.BlockCopy(nonceCiphertextTag, NonceSizeBytes + cipherLength, tag, 0, TagSizeBytes);

        var plaintext = new byte[cipherLength];
        using (var aesGcm = new AesGcm(key, TagSizeBytes))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        return plaintext;
    }
}
