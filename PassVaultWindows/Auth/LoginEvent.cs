namespace PassVaultWindows.Auth;

public enum LoginEventType
{
    Pattern,
    WindowsHello,
    Recovery
}

public class LoginEvent
{
    public long Timestamp { get; set; }
    public LoginEventType Type { get; set; }
    public bool Success { get; set; }
}
