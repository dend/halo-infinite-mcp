namespace OpenSpartan.Forerunner.MCP.Core
{
    internal sealed class Configuration
    {
        internal const string PackageName = "OpenSpartan.Forerunner.MCP";
        internal const string Version = "1.0.0";
        internal const string BuildId = "AIRWOLF-03222025";

        internal const string HaloInfiniteAPIRelease = "1.10";

        // Authentication and setting-related metadata.
        internal static readonly string[] Scopes = ["Xboxlive.signin", "Xboxlive.offline_access"];
        internal const string ClientID = "bfa30ae3-0299-45cb-b5fe-53cc9ac31325";
        internal const string CacheFileName = "authcache.bin";
        internal const string SettingsFileName = "settings.json";
        internal static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageName);
    }
}
