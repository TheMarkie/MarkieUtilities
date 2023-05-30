using System.Windows.Media;

namespace Updater
{
    public static class Constants
    {
        public const string TempDirectoryName = "Temp";
        public const string ObsoleteSuffix = ".old";

        // A shade of yellow.
        public static readonly Color OutdatedVersionColor = Color.FromRgb(255, 255, 115);
        // A shade of green.
        public static readonly Color UpdatedVersionColor = Color.FromRgb(138, 255, 69);

        // Config keys
        public const string ProjectNameKey = "ProjectName";
        public const string LatestVersionKey = "LatestVersion";
        public const string LatestVersionApiUriKey = "LatestVersionApiUri";
        public const string LatestVersionDescriptionUriKey = "LatestVersionDescriptionUri";
        public const string DiscordUriKey = "DiscordUri";

        // Version keys
        public const string ApiLatestVersionKey = "latest_version";
        public const string ApiLatestVersionUriKey = "latest_version_uri";
        public const string ApiLatestVersionPatchUriKey = "latest_version_patch_uri";
        public const string ApiLatestVersionDescriptionUriKey = "latest_version_description_uri";
    }
}
