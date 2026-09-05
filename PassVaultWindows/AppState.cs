using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PassVaultWindows.Auth;
using PassVaultWindows.Crypto;
using PassVaultWindows.Data;

namespace PassVaultWindows;

/// <summary>
/// Single shared holder for auth/vault state across the whole app - mirrors the Android app's
/// VaultViewModel.kt. Owns the encrypted-prefs store, the pattern/security-question/Windows
/// Hello managers, both repositories, and the in-memory DEK for the current session.
/// </summary>
public class AppState
{
    public AuthPrefs AuthPrefs { get; }
    public PatternAuthManager PatternAuth { get; }
    public SecurityQuestionManager SecurityQuestions { get; }
    public WindowsHelloAuthManager WindowsHello { get; }
    public VaultRepository VaultRepository { get; }
    public PhotoVaultRepository PhotoVaultRepository { get; }

    public event Action<bool>? IsUnlockedChanged;
    private bool _isUnlocked;
    public bool IsUnlocked
    {
        get => _isUnlocked;
        private set { _isUnlocked = value; IsUnlockedChanged?.Invoke(value); }
    }

    public event Action<int>? FailedAttemptsSinceLastLoginChanged;
    private int _failedAttemptsSinceLastLogin;
    public int FailedAttemptsSinceLastLogin
    {
        get => _failedAttemptsSinceLastLogin;
        private set { _failedAttemptsSinceLastLogin = value; FailedAttemptsSinceLastLoginChanged?.Invoke(value); }
    }

    // Only held transiently in memory while stepping through onboarding. Wiped as soon as
    // onboarding finishes (or the process exits, since it's never persisted).
    private byte[]? _onboardingDek;

    public bool IsOnboarded => AuthPrefs.OnboardingComplete;
    public bool IsWindowsHelloEnabled => AuthPrefs.WindowsHelloEnabled;

    public AppState()
    {
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PassVault");
        Directory.CreateDirectory(appDataDir);

        AuthPrefs = new AuthPrefs();
        PatternAuth = new PatternAuthManager(AuthPrefs);
        SecurityQuestions = new SecurityQuestionManager(AuthPrefs);
        WindowsHello = new WindowsHelloAuthManager();
        VaultRepository = new VaultRepository(appDataDir);
        PhotoVaultRepository = new PhotoVaultRepository(appDataDir);

        ApplyTheme(CurrentThemeMode());
    }

    // --- Onboarding ---
    // Pattern/security-question setup and verification all run PBKDF2 (210k iterations,
    // intentionally slow to resist brute-forcing) on a background thread via Task.Run inside
    // the managers/DekWrapper's callers below, so the UI never freezes while it computes.

    public async Task BeginOnboardingWithPatternAsync(List<int> pattern)
    {
        var dek = CryptoManager.GenerateRandomKey();
        _onboardingDek = dek;
        await Task.Run(() => PatternAuth.SetPattern(pattern, dek));
    }

    public async Task FinishOnboardingSecurityQuestionsAsync(List<string> questions, List<string> answers)
    {
        var dek = _onboardingDek ?? throw new InvalidOperationException("Pattern must be set before security questions");
        await Task.Run(() => SecurityQuestions.SetQuestions(questions, answers, dek));
    }

    public async Task FinishOnboardingAsync(bool enableWindowsHello)
    {
        var helloAvailable = enableWindowsHello && await WindowsHello.IsAvailableAsync();
        AuthPrefs.WindowsHelloEnabled = helloAvailable;
        AuthPrefs.OnboardingComplete = true;
        var dek = _onboardingDek;
        if (dek != null)
        {
            await VaultRepository.UnlockAsync(dek);
            await PhotoVaultRepository.UnlockAsync(dek);
            IsUnlocked = true;
        }
        _onboardingDek = null;
    }

    // --- Login ---

    /// <summary>Returns true if <paramref name="pattern"/> was correct and the vault is now unlocked.</summary>
    public async Task<bool> TryLoginWithPatternAsync(List<int> pattern)
    {
        var dek = await Task.Run(() => PatternAuth.TryUnlock(pattern));
        if (dek == null)
        {
            AuthPrefs.RecordLoginEvent(LoginEventType.Pattern, success: false);
            return false;
        }
        FailedAttemptsSinceLastLogin = FailedAttemptsSinceLastRecordedSuccess();
        AuthPrefs.RecordLoginEvent(LoginEventType.Pattern, success: true);
        await VaultRepository.UnlockAsync(dek);
        await PhotoVaultRepository.UnlockAsync(dek);
        IsUnlocked = true;
        return true;
    }

    public async Task<bool> VerifySecurityAnswersAsync(List<string> answers) =>
        await Task.Run(() => SecurityQuestions.TryUnlock(answers) != null);

    /// <summary>Recovery path: verifies security answers and, if correct, sets a brand new pattern.</summary>
    public async Task<bool> RecoverWithSecurityAnswersAsync(List<string> answers, List<int> newPattern)
    {
        var dek = await Task.Run(() =>
        {
            var unwrapped = SecurityQuestions.TryUnlock(answers);
            if (unwrapped == null)
            {
                return null;
            }
            PatternAuth.SetPattern(newPattern, unwrapped);
            return unwrapped;
        });
        if (dek == null)
        {
            return false;
        }
        FailedAttemptsSinceLastLogin = FailedAttemptsSinceLastRecordedSuccess();
        AuthPrefs.RecordLoginEvent(LoginEventType.Recovery, success: true);
        await VaultRepository.UnlockAsync(dek);
        await PhotoVaultRepository.UnlockAsync(dek);
        IsUnlocked = true;
        return true;
    }

    /// <summary>Call when Windows Hello reports an actual failed/declined verification (not "unavailable").</summary>
    public void RecordFailedWindowsHelloAttempt() => AuthPrefs.RecordLoginEvent(LoginEventType.WindowsHello, success: false);

    public void DismissFailedAttemptsBanner() => FailedAttemptsSinceLastLogin = 0;

    public List<LoginEvent> GetLoginEvents() => AuthPrefs.LoginEvents();

    private int FailedAttemptsSinceLastRecordedSuccess()
    {
        int count = 0;
        foreach (var evt in AuthPrefs.LoginEvents())
        {
            if (evt.Success)
            {
                break;
            }
            count++;
        }
        return count;
    }

    public void Lock()
    {
        VaultRepository.Lock();
        PhotoVaultRepository.Lock();
        IsUnlocked = false;
    }

    // --- Settings ---

    public async Task SetWindowsHelloEnabledAsync(bool enabled)
    {
        AuthPrefs.WindowsHelloEnabled = enabled && await WindowsHello.IsAvailableAsync();
    }

    public void SetAutoLockSeconds(int seconds) => AuthPrefs.AutoLockSeconds = seconds;

    public async Task ChangePatternAsync(List<int> newPattern)
    {
        var dek = VaultRepository.CurrentDek() ?? throw new InvalidOperationException("Vault must be unlocked to change pattern");
        await Task.Run(() => PatternAuth.SetPattern(newPattern, dek));
    }

    public async Task ChangeSecurityQuestionsAsync(List<string> questions, List<string> answers)
    {
        var dek = VaultRepository.CurrentDek() ?? throw new InvalidOperationException("Vault must be unlocked to change security questions");
        await Task.Run(() => SecurityQuestions.SetQuestions(questions, answers, dek));
    }

    public void EraseEverything()
    {
        Lock();
        AuthPrefs.ClearAll();
        VaultRepository.DeleteVaultFile();
        PhotoVaultRepository.DeleteAll();
    }

    // --- Theme ---

    public ThemeMode CurrentThemeMode() =>
        Enum.TryParse<ThemeMode>(AuthPrefs.ThemeMode, out var mode) ? mode : ThemeMode.System;

    public void SetThemeMode(ThemeMode mode)
    {
        AuthPrefs.ThemeMode = mode.ToString();
        ApplyTheme(mode);
    }

    private void ApplyTheme(ThemeMode mode)
    {
        bool dark = mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => IsSystemInDarkMode()
        };
        var uri = dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
        Application.Current.Resources.MergedDictionaries[0] = dict;
    }

    private static bool IsSystemInDarkMode()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return value is int i && i == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
