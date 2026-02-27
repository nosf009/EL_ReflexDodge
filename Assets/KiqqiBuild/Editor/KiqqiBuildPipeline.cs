using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using System;
using System.IO;
using System.Collections.Generic;

namespace Kiqqi.BuildSystem.Editor
{
    public enum BuildType
    {
        Mobile,
        Desktop
    }

    public class BuildTaskResult
    {
        public bool Success;
        public string ErrorMessage;
        public long BuildSizeBytes;
        public TimeSpan Duration;

        public static BuildTaskResult Ok(long size, TimeSpan duration)
        {
            return new BuildTaskResult { Success = true, BuildSizeBytes = size, Duration = duration };
        }

        public static BuildTaskResult Fail(string error)
        {
            return new BuildTaskResult { Success = false, ErrorMessage = error };
        }
    }

    public static class KiqqiBuildPipeline
    {
        public static BuildTaskResult BuildGame(
            GameBuildConfig config,
            BuildType buildType,
            string outputBasePath,
            string tempPath,
            KiqqiBuildSettings settings)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                BuildProfile profile = buildType == BuildType.Mobile ? config.mobileBuildProfile : config.desktopBuildProfile;
                string buildTypeName = buildType == BuildType.Mobile ? "mobile" : "desktop";

                if (profile == null)
                {
                    return BuildTaskResult.Fail($"No {buildTypeName} Build Profile assigned");
                }

                KiqqiBuildLogger.Log($"Building {config.gameId} ({buildTypeName})...");

                string tempBuildPath = Path.Combine(tempPath, config.gameId);
                if (Directory.Exists(tempBuildPath))
                    Directory.Delete(tempBuildPath, true);

                string originalProductName = PlayerSettings.productName;

                try
                {
                    PlayerSettings.productName = config.gameId;

                    EditorUserBuildSettings.SetPlatformSettings(
                        BuildPipeline.GetBuildTargetGroup(BuildTarget.WebGL).ToString(),
                        "buildProfile",
                        AssetDatabase.GetAssetPath(profile)
                    );

                    string[] scenePaths = new string[profile.scenes.Length];
                    for (int i = 0; i < profile.scenes.Length; i++)
                    {
                        scenePaths[i] = profile.scenes[i].path;
                    }

                    BuildPlayerOptions buildOptions = new BuildPlayerOptions
                    {
                        scenes = scenePaths,
                        locationPathName = tempBuildPath,
                        target = BuildTarget.WebGL,
                        options = BuildOptions.None
                    };

                    var report = BuildPipeline.BuildPlayer(buildOptions);

                    if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    {
                        return BuildTaskResult.Fail($"Build failed: {report.summary.result}");
                    }

                    KiqqiBuildLogger.Log($"Build completed, verifying...");

                    if (settings.verifyBuilds)
                    {
                        var verifyResult = KiqqiBuildVerifier.VerifyWebGLBuild(tempBuildPath, config.gameId);
                        if (!verifyResult.Success)
                        {
                            return BuildTaskResult.Fail($"Verification failed: {verifyResult.ErrorMessage}");
                        }
                    }

                    string finalOutputPath = Path.Combine(outputBasePath, config.gameId, buildTypeName);
                    if (Directory.Exists(finalOutputPath))
                        Directory.Delete(finalOutputPath, true);

                    Directory.CreateDirectory(Path.GetDirectoryName(finalOutputPath));
                    Directory.Move(tempBuildPath, finalOutputPath);

                    long buildSize = KiqqiBuildVerifier.GetBuildSize(finalOutputPath);
                    TimeSpan duration = DateTime.Now - startTime;

                    KiqqiBuildLogger.Log($"✓ {config.gameId} ({buildTypeName}) - {KiqqiBuildVerifier.FormatBytes(buildSize)} in {duration.TotalSeconds:F1}s");

                    return BuildTaskResult.Ok(buildSize, duration);
                }
                finally
                {
                    PlayerSettings.productName = originalProductName;
                }
            }
            catch (Exception e)
            {
                KiqqiBuildLogger.LogError($"Exception during build: {e.Message}");
                return BuildTaskResult.Fail(e.Message);
            }
        }

        public static bool CopyArtFolder(GameBuildConfig config, string projectRootPath, string outputBasePath)
        {
            try
            {
                string artSourcePath = Path.Combine(projectRootPath, config.artFolderName);

                if (!Directory.Exists(artSourcePath))
                {
                    KiqqiBuildLogger.LogWarning($"Art folder not found for {config.gameId}: {artSourcePath} (skipping)");
                    return false;
                }

                string artDestPath = Path.Combine(outputBasePath, config.gameId, config.artFolderName);

                if (Directory.Exists(artDestPath))
                    Directory.Delete(artDestPath, true);

                CopyDirectory(artSourcePath, artDestPath);

                KiqqiBuildLogger.Log($"Copied {config.artFolderName} folder for {config.gameId}");
                return true;
            }
            catch (Exception e)
            {
                KiqqiBuildLogger.LogError($"Failed to copy art folder for {config.gameId}: {e.Message}");
                return false;
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        public static void CleanTempDirectory(string tempPath)
        {
            try
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                    KiqqiBuildLogger.Log("Cleaned temporary build directory");
                }
            }
            catch (Exception e)
            {
                KiqqiBuildLogger.LogWarning($"Failed to clean temp directory: {e.Message}");
            }
        }
    }
}
