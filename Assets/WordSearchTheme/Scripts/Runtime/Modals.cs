/// <summary>Runtime panels that sit above every scene. Screens below must not react to Android back while one is open.</summary>
public static class Modals
{
    public static bool AnyOpen => ConsentGate.IsOpen || NoInternetPanel.IsOpen || RemoveAdsOffer.IsOpen;
}
