using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PassVaultWindows.Data;

/// <summary>Metadata only - the actual encrypted image bytes live in their own file, named by <see cref="Id"/>.</summary>
public class VaultPhoto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Caption { get; set; } = "";
    public long AddedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static string ListToJson(List<VaultPhoto> photos) => JsonSerializer.Serialize(photos, Credential.JsonOptions);

    public static List<VaultPhoto> ListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<VaultPhoto>();
        }
        return JsonSerializer.Deserialize<List<VaultPhoto>>(json, Credential.JsonOptions) ?? new List<VaultPhoto>();
    }
}
