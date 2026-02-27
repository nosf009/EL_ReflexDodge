using UnityEngine;
using System.IO;

namespace Kiqqi.BuildSystem.Editor
{
    public class BuildVerificationResult
    {
        public bool Success;
        public string ErrorMessage;

        public static BuildVerificationResult Ok()
        {
            return new BuildVerificationResult { Success = true };
        }

        public static BuildVerificationResult Fail(string error)
        {
            return new BuildVerificationResult { Success = false, ErrorMessage = error };
        }
    }

    public static class KiqqiBuildVerifier
    {
        public static BuildVerificationResult VerifyWebGLBuild(string buildPath, string expectedProductName)
        {
            if (!Directory.Exists(buildPath))
                return BuildVerificationResult.Fail($"Build directory not found: {buildPath}");

            string indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath))
                return BuildVerificationResult.Fail($"index.html not found at: {indexPath}");

            string buildFolderPath = Path.Combine(buildPath, "Build");
            if (!Directory.Exists(buildFolderPath))
                return BuildVerificationResult.Fail($"Build folder not found: {buildFolderPath}");

            string[] requiredFiles = new string[]
            {
                Path.Combine(buildFolderPath, $"{expectedProductName}.data"),
                Path.Combine(buildFolderPath, $"{expectedProductName}.wasm"),
                Path.Combine(buildFolderPath, $"{expectedProductName}.loader.js"),
                Path.Combine(buildFolderPath, $"{expectedProductName}.framework.js")
            };

            foreach (string file in requiredFiles)
            {
                if (!File.Exists(file))
                    return BuildVerificationResult.Fail($"Required file not found: {Path.GetFileName(file)}");

                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Length == 0)
                    return BuildVerificationResult.Fail($"File is empty: {Path.GetFileName(file)}");
            }

            string indexContent = File.ReadAllText(indexPath);
            if (!indexContent.Contains($"productName: \"{expectedProductName}\""))
            {
                return BuildVerificationResult.Fail($"index.html does not contain expected productName: {expectedProductName}");
            }

            return BuildVerificationResult.Ok();
        }

        public static long GetBuildSize(string buildPath)
        {
            if (!Directory.Exists(buildPath))
                return 0;

            long totalSize = 0;
            DirectoryInfo dirInfo = new DirectoryInfo(buildPath);

            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                totalSize += file.Length;
            }

            return totalSize;
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.#} {sizes[order]}";
        }
    }
}
