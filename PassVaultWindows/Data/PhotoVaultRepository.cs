using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PassVaultWindows.Crypto;

namespace PassVaultWindows.Data;

/// <summary>
/// A separate encrypted photo store, sharing the same DEK as the password vault (no extra
/// pattern/key to manage) but keeping photo bytes out of the credentials file entirely - each
/// photo is its own encrypted file on disk, with a small metadata index listing them.
/// </summary>
public class PhotoVaultRepository
{
    private readonly string _indexFilePath;
    private readonly string _photosDir;
    private byte[]? _dek;

    public event Action? PhotosChanged;
    public List<VaultPhoto> Photos { get; private set; } = new();

    public PhotoVaultRepository(string appDataDir)
    {
        _indexFilePath = Path.Combine(appDataDir, "photos_index.dat");
        _photosDir = Path.Combine(appDataDir, "photos");
        Directory.CreateDirectory(_photosDir);
    }

    public async Task UnlockAsync(byte[] dek)
    {
        _dek = dek;
        Photos = await Task.Run(() => LoadIndex(dek));
        PhotosChanged?.Invoke();
    }

    public void Lock()
    {
        _dek = null;
        Photos = new List<VaultPhoto>();
        PhotosChanged?.Invoke();
    }

    public async Task AddPhotoAsync(string caption, byte[] imageBytes)
    {
        var key = _dek;
        if (key == null)
        {
            return;
        }
        var photo = new VaultPhoto { Caption = caption };
        await Task.Run(() =>
        {
            var encrypted = CryptoManager.Encrypt(imageBytes, key);
            File.WriteAllBytes(Path.Combine(_photosDir, photo.Id), encrypted);
        });
        Photos.Add(photo);
        PersistIndex();
        PhotosChanged?.Invoke();
    }

    public async Task DeletePhotoAsync(string id)
    {
        await Task.Run(() =>
        {
            var path = Path.Combine(_photosDir, id);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        });
        Photos.RemoveAll(p => p.Id == id);
        PersistIndex();
        PhotosChanged?.Invoke();
    }

    public async Task<byte[]?> LoadPhotoBytesAsync(string id)
    {
        var key = _dek;
        if (key == null)
        {
            return null;
        }
        return await Task.Run(() =>
        {
            try
            {
                var path = Path.Combine(_photosDir, id);
                if (!File.Exists(path))
                {
                    return null;
                }
                return CryptoManager.Decrypt(File.ReadAllBytes(path), key);
            }
            catch (Exception)
            {
                return null;
            }
        });
    }

    public void DeleteAll()
    {
        if (Directory.Exists(_photosDir))
        {
            foreach (var file in Directory.GetFiles(_photosDir))
            {
                File.Delete(file);
            }
        }
        if (File.Exists(_indexFilePath))
        {
            File.Delete(_indexFilePath);
        }
    }

    private List<VaultPhoto> LoadIndex(byte[] key)
    {
        if (!File.Exists(_indexFilePath) || new FileInfo(_indexFilePath).Length == 0)
        {
            return new List<VaultPhoto>();
        }
        try
        {
            var encrypted = File.ReadAllBytes(_indexFilePath);
            var plaintext = CryptoManager.Decrypt(encrypted, key);
            return VaultPhoto.ListFromJson(Encoding.UTF8.GetString(plaintext));
        }
        catch (Exception)
        {
            return new List<VaultPhoto>();
        }
    }

    private void PersistIndex()
    {
        var key = _dek;
        if (key == null)
        {
            return;
        }
        var json = VaultPhoto.ListToJson(Photos);
        var encrypted = CryptoManager.Encrypt(Encoding.UTF8.GetBytes(json), key);
        File.WriteAllBytes(_indexFilePath, encrypted);
    }
}
