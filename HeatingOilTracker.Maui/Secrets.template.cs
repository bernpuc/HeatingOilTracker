namespace HeatingOilTracker.Maui;

// Copy this file to Secrets.cs and fill in your values.
internal static class Secrets
{
    // Base URL (including scheme and port) of the HeatingOilTracker.Server instance running on
    // kassandra, reachable over Tailscale from both the Windows and Android builds. See
    // HeatingOilTracker.Server/README.md for how to deploy it and KASSANDRA_SYNC_PLAN.md for why.
    public const string KassandraSyncBaseUrl = "http://100.x.x.x:8086";
}
