using PassVaultWindows.Crypto;

namespace PassVaultWindows.Auth;

/// <summary>
/// Handles the hidden vault's own pattern - a second pattern, separate from the main login
/// pattern, that wraps a separate DEK for the hidden notes store. Mirrors
/// <see cref="PatternAuthManager"/> exactly, just pointed at a different slot in
/// <see cref="AuthPrefs"/> so the two are fully independent: neither pattern can unlock the
/// other vault.
/// </summary>
public class HiddenVaultAuthManager
{
    private readonly AuthPrefs _authPrefs;

    public HiddenVaultAuthManager(AuthPrefs authPrefs)
    {
        _authPrefs = authPrefs;
    }

    public bool HasHiddenVault() => _authPrefs.HiddenPatternSalt() != null && _authPrefs.HiddenPatternWrappedDek() != null;

    /// <summary>Wraps <paramref name="dek"/> under a freshly derived key from <paramref name="pattern"/> and persists it.</summary>
    public void SetPattern(List<int> pattern, byte[] dek)
    {
        if (pattern.Count < PatternAuthManager.MinPatternLength)
        {
            throw new ArgumentException($"Pattern must connect at least {PatternAuthManager.MinPatternLength} dots");
        }
        var salt = KeyDerivation.NewSalt();
        var wrapped = DekWrapper.Wrap(dek, PatternToSecret(pattern), salt);
        _authPrefs.SaveHiddenPatternWrap(salt, wrapped);
    }

    /// <summary>Returns the DEK if <paramref name="pattern"/> matches the hidden vault's pattern, or null if it doesn't.</summary>
    public byte[]? TryUnlock(List<int> pattern)
    {
        var salt = _authPrefs.HiddenPatternSalt();
        var wrapped = _authPrefs.HiddenPatternWrappedDek();
        if (salt == null || wrapped == null)
        {
            return null;
        }
        return DekWrapper.TryUnwrap(wrapped, PatternToSecret(pattern), salt);
    }

    private static string PatternToSecret(List<int> pattern) => string.Join(",", pattern);
}
