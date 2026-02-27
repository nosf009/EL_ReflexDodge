using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace Kiqqi.BuildSystem.Editor
{
    [System.Serializable]
    public class GameBuildConfig
    {
        public string gameId = "MyGame";
        public string displayName = "My Game";
        public BuildProfile mobileBuildProfile;
        public BuildProfile desktopBuildProfile;
        public string artFolderName = "_art";

        public bool HasMobileProfile => mobileBuildProfile != null;
        public bool HasDesktopProfile => desktopBuildProfile != null;
    }

    [CreateAssetMenu(fileName = "KiqqiBuildSettings", menuName = "Kiqqi/Build Settings", order = 0)]
    public class KiqqiBuildSettings : ScriptableObject
    {
        [Header("Paths")]
        [Tooltip("Full disk path where builds will be output (e.g., D:/KiqqiBuilds/)")]
        public string buildRootPath = "";

        [Tooltip("Project root path (auto-detected, override if needed)")]
        public string projectRootPath = "";

        [Tooltip("Art folder name in project root (default: _art)")]
        public string artSourceFolderName = "_art";

        [Header("Build Options")]
        [Tooltip("Verify builds after completion (check productName, files exist)")]
        public bool verifyBuilds = true;

        [Tooltip("Copy _art folders from project root to build output")]
        public bool copyArtFolders = true;

        [Tooltip("Create ZIP archives for each game after build")]
        public bool createZipArchives = false;

        [Tooltip("Clean temporary files after build completes")]
        public bool cleanTempOnComplete = true;

        [Tooltip("Build timeout in seconds (0 = no timeout)")]
        public int buildTimeoutSeconds = 300;

        [Header("Game Configuration")]
        public GameBuildConfig gameConfig = new GameBuildConfig();

        public string TempBuildPath => System.IO.Path.Combine(buildRootPath, "_Temp");
    }
}
