using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace PassVaultWindows.Auth;

/// <summary>
/// Thin wrapper around Windows Hello (fingerprint/face/PIN, whatever's enrolled). This is a
/// device-level "something you are" gate shown before the pattern step - it does NOT derive
/// key material from it (not reliable to do so); the actual vault key comes from the
/// pattern/security-answer KDF instead. If Windows Hello isn't set up - or this specific WinRT
/// API can't fully activate without package identity on some Windows configurations -
/// <see cref="IsAvailableAsync"/> simply returns false and the app falls back to pattern-only
/// login, exactly like the Android app does when no biometric hardware is present.
/// </summary>
public class WindowsHelloAuthManager
{
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string message = "Unlock PassVault")
    {
        try
        {
            var result = await UserConsentVerifier.RequestVerificationAsync(message);
            return result == UserConsentVerificationResult.Verified;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
