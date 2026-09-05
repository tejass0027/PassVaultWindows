namespace PassVaultWindows.Crypto;

/// <summary>
/// Wraps/unwraps the vault's Data Encryption Key (DEK) under a key derived from a low-entropy
/// secret (pattern or security answers). Used by both the pattern and security-question
/// recovery paths so the same DEK - and therefore the same encrypted vault - stays reachable
/// from either route.
/// </summary>
public static class DekWrapper
{
    public static byte[] Wrap(byte[] dek, string secret, byte[] salt)
    {
        var wrappingKey = KeyDerivation.DeriveKey(secret, salt);
        try
        {
            return CryptoManager.Encrypt(dek, wrappingKey);
        }
        finally
        {
            Array.Clear(wrappingKey, 0, wrappingKey.Length);
        }
    }

    /// <summary>Returns null if <paramref name="secret"/> is wrong (authentication tag mismatch) rather than throwing.</summary>
    public static byte[]? TryUnwrap(byte[] wrappedDek, string secret, byte[] salt)
    {
        var wrappingKey = KeyDerivation.DeriveKey(secret, salt);
        try
        {
            return CryptoManager.Decrypt(wrappedDek, wrappingKey);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            Array.Clear(wrappingKey, 0, wrappingKey.Length);
        }
    }
}
