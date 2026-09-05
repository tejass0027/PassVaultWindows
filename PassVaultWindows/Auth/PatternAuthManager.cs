using PassVaultWindows.Crypto;

namespace PassVaultWindows.Auth;

/// <summary>
/// Handles the pattern-lock side of authentication: wrapping/unwrapping the vault DEK under a
/// key derived from the drawn pattern. The pattern itself is never stored - only a salt and the
/// resulting wrapped DEK, so there is nothing to reverse-engineer the pattern from short of
/// brute-forcing the KDF.
/// </summary>
public class PatternAuthManager
{
    public const int MinPatternLength = 4;

    private readonly AuthPrefs _authPrefs;

    public PatternAuthManager(AuthPrefs authPrefs)
    {
        _authPrefs = authPrefs;
    }

    public bool HasPattern() => _authPrefs.PatternSalt() != null && _authPrefs.PatternWrappedDek() != null;

    /// <summary>Wraps <paramref name="dek"/> under a freshly derived key from <paramref name="pattern"/> and persists it.</summary>
    public void SetPattern(List<int> pattern, byte[] dek)
    {
        if (pattern.Count < MinPatternLength)
        {
            throw new ArgumentException($"Pattern must connect at least {MinPatternLength} dots");
        }
        var salt = KeyDerivation.NewSalt();
        var wrapped = DekWrapper.Wrap(dek, PatternToSecret(pattern), salt);
        _authPrefs.SavePatternWrap(salt, wrapped);
    }

    /// <summary>Returns the DEK if <paramref name="pattern"/> is correct, or null if it doesn't match.</summary>
    public byte[]? TryUnlock(List<int> pattern)
    {
        var salt = _authPrefs.PatternSalt();
        var wrapped = _authPrefs.PatternWrappedDek();
        if (salt == null || wrapped == null)
        {
            return null;
        }
        return DekWrapper.TryUnwrap(wrapped, PatternToSecret(pattern), salt);
    }

    private static string PatternToSecret(List<int> pattern) => string.Join(",", pattern);
}
