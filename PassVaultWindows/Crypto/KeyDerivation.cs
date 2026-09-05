using System.Security.Cryptography;
using System.Text;

namespace PassVaultWindows.Crypto;

/// <summary>
/// Derives a symmetric key from low-entropy secrets (a drawn pattern, security answers, a
/// backup password) via PBKDF2-HMAC-SHA256, using the same iteration count and key length as
/// the Android app's KeyDerivation.kt so a .pvbk backup exported from either platform decrypts
/// on the other. The derived key only ever wraps/unwraps the vault's real Data Encryption Key -
/// it never touches vault data directly.
///
/// Note: for best cross-platform compatibility, use plain ASCII characters (letters, numbers,
/// common symbols) in pattern-independent secrets like a backup password or security answers -
/// Android and .NET could theoretically differ in how they byte-encode non-ASCII characters
/// before hashing, though both use UTF-8 here.
/// </summary>
public static class KeyDerivation
{
    private const int Iterations = 210_000;
    private const int KeyLengthBytes = 32;
    public const int SaltSizeBytes = 16;

    public static byte[] DeriveKey(string secret, byte[] salt)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        return Rfc2898DeriveBytes.Pbkdf2(secretBytes, salt, Iterations, HashAlgorithmName.SHA256, KeyLengthBytes);
    }

    public static byte[] NewSalt() => CryptoManager.RandomBytes(SaltSizeBytes);
}
