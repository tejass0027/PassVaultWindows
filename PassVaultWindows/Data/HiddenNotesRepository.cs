using System.IO;
using System.Text;
using PassVaultWindows.Crypto;

namespace PassVaultWindows.Data;

/// <summary>
/// Holds the hidden notes vault, encrypted with its own DEK that is completely separate from
/// the main password vault's - wrapped only by the hidden vault's own pattern, never the main
/// one. Mirrors <see cref="VaultRepository"/> in every other respect.
/// </summary>
public class HiddenNotesRepository
{
    private readonly string _notesFilePath;
    private byte[]? _dek;

    public event Action? NotesChanged;
    public List<HiddenNote> Notes { get; private set; } = new();

    public bool IsUnlocked => _dek != null;

    public HiddenNotesRepository(string appDataDir)
    {
        _notesFilePath = Path.Combine(appDataDir, "hidden_notes.dat");
    }

    public async Task UnlockAsync(byte[] dek)
    {
        _dek = dek;
        Notes = await Task.Run(() => LoadFromDisk(dek));
        NotesChanged?.Invoke();
    }

    public void Lock()
    {
        if (_dek != null)
        {
            Array.Clear(_dek, 0, _dek.Length);
        }
        _dek = null;
        Notes = new List<HiddenNote>();
        NotesChanged?.Invoke();
    }

    public async Task UpsertAsync(HiddenNote note)
    {
        var index = Notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0)
        {
            Notes[index] = note;
        }
        else
        {
            Notes.Add(note);
        }
        NotesChanged?.Invoke();
        await PersistAsync();
    }

    public async Task DeleteAsync(string id)
    {
        Notes.RemoveAll(n => n.Id == id);
        NotesChanged?.Invoke();
        await PersistAsync();
    }

    public void DeleteVaultFile()
    {
        if (File.Exists(_notesFilePath))
        {
            File.Delete(_notesFilePath);
        }
    }

    private List<HiddenNote> LoadFromDisk(byte[] key)
    {
        if (!File.Exists(_notesFilePath) || new FileInfo(_notesFilePath).Length == 0)
        {
            return new List<HiddenNote>();
        }
        try
        {
            var encrypted = File.ReadAllBytes(_notesFilePath);
            var plaintext = CryptoManager.Decrypt(encrypted, key);
            return HiddenNote.ListFromJson(Encoding.UTF8.GetString(plaintext));
        }
        catch (Exception)
        {
            return new List<HiddenNote>();
        }
    }

    private Task PersistAsync()
    {
        var key = _dek;
        if (key == null)
        {
            return Task.CompletedTask;
        }
        var snapshot = new List<HiddenNote>(Notes);
        return Task.Run(() =>
        {
            var json = HiddenNote.ListToJson(snapshot);
            var encrypted = CryptoManager.Encrypt(Encoding.UTF8.GetBytes(json), key);
            File.WriteAllBytes(_notesFilePath, encrypted);
        });
    }
}
