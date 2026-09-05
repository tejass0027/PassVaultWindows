using System.Linq;
using PassVaultWindows.Crypto;

namespace PassVaultWindows.Auth;

/// <summary>
/// Recovery path for when the user can't log in with Windows Hello + pattern (new PC, forgot
/// the pattern, etc). Answers are never stored - like the pattern, they only ever exist
/// transiently to derive a key that unwraps the same DEK used everywhere else, so recovering
/// via security questions and logging in via pattern reach the exact same vault.
/// </summary>
public class SecurityQuestionManager
{
    public const int RequiredQuestionCount = 3;

    public static readonly List<string> SuggestedQuestions = new()
    {
        "What was the name of your first pet?",
        "What city were you born in?",
        "What was the model of your first car?",
        "What is your mother's maiden name?",
        "What was the name of your first school?",
        "What is your favorite childhood nickname?"
    };

    private readonly AuthPrefs _authPrefs;

    public SecurityQuestionManager(AuthPrefs authPrefs)
    {
        _authPrefs = authPrefs;
    }

    public bool HasSecurityQuestions() => _authPrefs.SecuritySalt() != null && _authPrefs.SecurityWrappedDek() != null;

    public List<string> Questions() => _authPrefs.SecurityQuestions();

    /// <summary>Wraps <paramref name="dek"/> under a key derived from <paramref name="answers"/> (same order as <paramref name="questions"/>) and persists it.</summary>
    public void SetQuestions(List<string> questions, List<string> answers, byte[] dek)
    {
        if (questions.Count != RequiredQuestionCount || answers.Count != RequiredQuestionCount)
        {
            throw new ArgumentException($"Exactly {RequiredQuestionCount} security questions are required");
        }
        var salt = KeyDerivation.NewSalt();
        var wrapped = DekWrapper.Wrap(dek, AnswersToSecret(answers), salt);
        _authPrefs.SaveSecurityWrap(salt, wrapped, questions);
    }

    /// <summary>Returns the DEK if <paramref name="answers"/> (same order as <see cref="Questions"/>) are correct, or null otherwise.</summary>
    public byte[]? TryUnlock(List<string> answers)
    {
        var salt = _authPrefs.SecuritySalt();
        var wrapped = _authPrefs.SecurityWrappedDek();
        if (salt == null || wrapped == null)
        {
            return null;
        }
        return DekWrapper.TryUnwrap(wrapped, AnswersToSecret(answers), salt);
    }

    private static string AnswersToSecret(List<string> answers) =>
        string.Join("|", answers.Select(a => a.Trim().ToLowerInvariant()));
}
