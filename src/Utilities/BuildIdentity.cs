namespace Lotus.Utilities;

public static class BuildIdentity
{
    public const string SupportedGame = "Among Us 19.0.0 (2026.9.29)";
    public static readonly string Display = $"Lotus {ProjectLotus.PluginVersion} — {ProjectLotus.DevVersionStr}\n"
        + $"Build {typeof(ProjectLotus).Assembly.ManifestModule.ModuleVersionId.ToString("N")[..12]} | Target: {SupportedGame}";
}
