using System.Text.Json;

namespace PassVaultWindows.Data;

public class HiddenNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public long UpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static string ListToJson(List<HiddenNote> notes) => JsonSerializer.Serialize(notes, Credential.JsonOptions);

    public static List<HiddenNote> ListFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<HiddenNote>();
        }
        return JsonSerializer.Deserialize<List<HiddenNote>>(json, Credential.JsonOptions) ?? new List<HiddenNote>();
    }
}
