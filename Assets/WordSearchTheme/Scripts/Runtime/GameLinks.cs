/// <summary>
/// The one place for every outside link. Fill these in before release:
/// the consent gate, the Settings screen and the main-menu tiles all read them.
/// </summary>
public static class GameLinks
{
    public const string PrivacyPolicy = "https://example.com/privacy";
    public const string TermsOfService = "https://example.com/terms";
    /// <summary>Your Google Play developer page.</summary>
    public const string MoreGames = "https://play.google.com/store/apps/developer?id=YOUR_DEV_NAME";

    public static void Open(string url, string what)
    {
        if (string.IsNullOrEmpty(url) || url.Contains("example.com") || url.Contains("YOUR_DEV_NAME"))
            UnityEngine.Debug.LogError($"[GameLinks] {what} URL is still a placeholder - set it in GameLinks.cs");
        if (!string.IsNullOrEmpty(url)) UnityEngine.Application.OpenURL(url);
    }
}
