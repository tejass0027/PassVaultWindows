using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassVaultWindows.Crypto;

namespace PassVaultWindows.Data;

/// <summary>
/// Holds the unlocked vault in memory for the current session and persists it to a single
/// AES-GCM encrypted file (vault.dat) in per-user app data storage. Nothing here ever touches
/// disk in plaintext. Call <see cref="Lock"/> to drop the DEK and credentials from memory.
/// Encryption and file I/O run on a background thread via <see cref="Task.Run"/> so they never
/// block the UI thread - e.g. saving an entry shouldn't stall the window.
/// </summary>
public class VaultRepository
{
    private readonly string _vaultFilePath;
    private byte[]? _dek;

    public event Action? CredentialsChanged;
    public List<Credential> Credentials { get; private set; } = new();

    public bool IsUnlocked => _dek != null;

    public VaultRepository(string appDataDir)
    {
        _vaultFilePath = Path.Combine(appDataDir, "vault.dat");
    }

    public async Task UnlockAsync(byte[] dek)
    {
        _dek = dek;
        Credentials = await Task.Run(() => LoadFromDisk(dek));
        CredentialsChanged?.Invoke();
    }

    public void Lock()
    {
        if (_dek != null)
        {
            Array.Clear(_dek, 0, _dek.Length);
        }
        _dek = null;
        Credentials = new List<Credential>();
        CredentialsChanged?.Invoke();
    }

    public byte[]? CurrentDek() => _dek;

    public async Task UpsertAsync(Credential credential)
    {
        var index = Credentials.FindIndex(c => c.Id == credential.Id);
        if (index >= 0)
        {
            Credentials[index] = credential;
        }
        else
        {
            Credentials.Add(credential);
        }
        CredentialsChanged?.Invoke();
        await PersistAsync();
    }

    public async Task DeleteAsync(string id)
    {
        Credentials.RemoveAll(c => c.Id == id);
        CredentialsChanged?.Invoke();
        await PersistAsync();
    }

    public async Task ReplaceAllAsync(List<Credential> newCredentials)
    {
        Credentials = newCredentials;
        CredentialsChanged?.Invoke();
        await PersistAsync();
    }

    public void DeleteVaultFile()
    {
        if (File.Exists(_vaultFilePath))
        {
            File.Delete(_vaultFilePath);
        }
    }

    private List<Credential> LoadFromDisk(byte[] key)
    {
        if (!File.Exists(_vaultFilePath) || new FileInfo(_vaultFilePath).Length == 0)
        {
            return new List<Credential>();
        }
        try
        {
            var encrypted = File.ReadAllBytes(_vaultFilePath);
            var plaintext = CryptoManager.Decrypt(encrypted, key);
            return Credential.ListFromJson(Encoding.UTF8.GetString(plaintext));
        }
        catch (Exception)
        {
            return new List<Credential>();
        }
    }

    private Task PersistAsync()
    {
        var key = _dek;
        if (key == null)
        {
            return Task.CompletedTask;
        }
        var snapshot = new List<Credential>(Credentials);
        return Task.Run(() =>
        {
            var json = Credential.ListToJson(snapshot);
            var encrypted = CryptoManager.Encrypt(Encoding.UTF8.GetBytes(json), key);
            File.WriteAllBytes(_vaultFilePath, encrypted);
        });
    }
}
