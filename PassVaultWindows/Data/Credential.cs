using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassVaultWindows.Data;

public class Credential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Url { get; set; } = "";
    public string Notes { get; set; } = "";
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // camelCase to match the exact JSON field names the Android app's Credential.kt produces,
    // so a .pvbk backup exported from either platform imports cleanly on the other.
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ListToJson(List<Credential> credentials) => JsonSerializer.Serialize(credentials, JsonOptions);

    public static List<Credential> ListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Credential>();
        }
        return JsonSerializer.Deserialize<List<Credential>>(json, JsonOptions) ?? new List<Credential>();
    }
}
