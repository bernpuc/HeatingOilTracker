namespace HeatingOilTracker.Maui;

// Copy this file to Secrets.cs and fill in your values.
internal static class Secrets
{
    // Base URL (including scheme and port) of the HeatingOilTracker.Server instance running on
    // kassandra. Split by platform because Windows (always on the home LAN) and Android (also
    // used off-LAN over cellular) reach it differently:
    //   - Windows isn't joined to the Tailscale tailnet, so it uses the LAN mDNS hostname.
    //   - Android IS joined to the tailnet (needed for off-LAN access), so it uses the
    //     Tailscale IP, which also works fine on the home LAN.
    // See HeatingOilTracker.Server/README.md for how to deploy the server and
    // KASSANDRA_SYNC_PLAN.md for why this exists.
    public const string KassandraSyncBaseUrlWindows = "http://kassandra.local:8086";
    public const string KassandraSyncBaseUrlAndroid = "http://100.x.x.x:8086";
}
