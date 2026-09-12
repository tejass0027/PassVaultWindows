using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace PassVaultWindows.Auth;

/// <summary>
/// Stores all non-secret vault metadata (salts, wrapped DEK copies, security question text,
/// settings, login activity) in a single JSON file protected by the Windows Data Protection
/// API (tied to the current Windows user account) - the Windows analogue of the Android app's
/// Keystore-backed EncryptedSharedPreferences. Never stores the DEK or any plaintext vault data.
/// </summary>
public class AuthPrefs
{
    private const int MaxLoginEvents = 50;

    private readonly string _filePath;
    private AuthPrefsData _data;

    public AuthPrefs()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PassVault");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "auth_prefs.dat");
        _data = Load();
    }

    public bool OnboardingComplete
    {
        get => _data.OnboardingComplete;
        set { _data.OnboardingComplete = value; Save(); }
    }

    public bool WindowsHelloEnabled
    {
        get => _data.WindowsHelloEnabled;
        set { _data.WindowsHelloEnabled = value; Save(); }
    }

    public int AutoLockSeconds
    {
        get => _data.AutoLockSeconds;
        set { _data.AutoLockSeconds = value; Save(); }
    }

    public string ThemeMode
    {
        get => _data.ThemeMode;
        set { _data.ThemeMode = value; Save(); }
    }

    public void SavePatternWrap(byte[] salt, byte[] wrappedDek)
    {
        _data.PatternSalt = Convert.ToBase64String(salt);
        _data.PatternWrappedDek = Convert.ToBase64String(wrappedDek);
        Save();
    }

    public byte[]? PatternSalt() => _data.PatternSalt is null ? null : Convert.FromBase64String(_data.PatternSalt);
    public byte[]? PatternWrappedDek() => _data.PatternWrappedDek is null ? null : Convert.FromBase64String(_data.PatternWrappedDek);

    public void SaveSecurityWrap(byte[] salt, byte[] wrappedDek, List<string> questions)
    {
        _data.SecuritySalt = Convert.ToBase64String(salt);
        _data.SecurityWrappedDek = Convert.ToBase64String(wrappedDek);
        _data.SecurityQuestions = questions;
        Save();
    }

    public byte[]? SecuritySalt() => _data.SecuritySalt is null ? null : Convert.FromBase64String(_data.SecuritySalt);
    public byte[]? SecurityWrappedDek() => _data.SecurityWrappedDek is null ? null : Convert.FromBase64String(_data.SecurityWrappedDek);
    public List<string> SecurityQuestions() => _data.SecurityQuestions;

    /// <summary>
    /// A second, separate pattern that unlocks a hidden notes vault with its own DEK - entirely
    /// independent of the main vault. Drawing this pattern on the login screen (instead of the
    /// main one) is the only way to reach it; there is no menu entry that reveals its contents.
    /// </summary>
    public void SaveHiddenPatternWrap(byte[] salt, byte[] wrappedDek)
    {
        _data.HiddenPatternSalt = Convert.ToBase64String(salt);
        _data.HiddenPatternWrappedDek = Convert.ToBase64String(wrappedDek);
        Save();
    }

    public byte[]? HiddenPatternSalt() => _data.HiddenPatternSalt is null ? null : Convert.FromBase64String(_data.HiddenPatternSalt);
    public byte[]? HiddenPatternWrappedDek() => _data.HiddenPatternWrappedDek is null ? null : Convert.FromBase64String(_data.HiddenPatternWrappedDek);

    public void ClearHiddenVault()
    {
        _data.HiddenPatternSalt = null;
        _data.HiddenPatternWrappedDek = null;
        Save();
    }

    public void RecordLoginEvent(LoginEventType type, bool success)
    {
        _data.LoginEvents.Insert(0, new LoginEvent
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Type = type,
            Success = success
        });
        while (_data.LoginEvents.Count > MaxLoginEvents)
        {
            _data.LoginEvents.RemoveAt(_data.LoginEvents.Count - 1);
        }
        Save();
    }

    public List<LoginEvent> LoginEvents() => _data.LoginEvents;

    public void ClearAll()
    {
        _data = new AuthPrefsData();
        Save();
    }

    private AuthPrefsData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AuthPrefsData();
            }
            var protectedBytes = File.ReadAllBytes(_filePath);
            var jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var data = JsonSerializer.Deserialize<AuthPrefsData>(jsonBytes);
            return data ?? new AuthPrefsData();
        }
        catch (Exception)
        {
            return new AuthPrefsData();
        }
    }

    private void Save()
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(_data);
        var protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedBytes);
    }

    private class AuthPrefsData
    {
        public bool OnboardingComplete { get; set; }
        public bool WindowsHelloEnabled { get; set; }
        public int AutoLockSeconds { get; set; } = 30;
        public string ThemeMode { get; set; } = "System";
        public string? PatternSalt { get; set; }
        public string? PatternWrappedDek { get; set; }
        public string? SecuritySalt { get; set; }
        public string? SecurityWrappedDek { get; set; }
        public List<string> SecurityQuestions { get; set; } = new();
        public string? HiddenPatternSalt { get; set; }
        public string? HiddenPatternWrappedDek { get; set; }
        public List<LoginEvent> LoginEvents { get; set; } = new();
    }
}
