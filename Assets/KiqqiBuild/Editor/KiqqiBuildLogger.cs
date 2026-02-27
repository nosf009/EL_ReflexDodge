using UnityEngine;
using System;
using System.IO;
using System.Text;

namespace Kiqqi.BuildSystem.Editor
{
    public static class KiqqiBuildLogger
    {
        private static string logFilePath;
        private static StringBuilder logBuffer = new StringBuilder();

        public static void StartNewLog(string buildRootPath)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logFileName = $"BuildLog_{timestamp}.txt";
            logFilePath = Path.Combine(buildRootPath, logFileName);

            logBuffer.Clear();
            Log($"=== KIQQI BUILD SYSTEM LOG ===");
            Log($"Started: {DateTime.Now}");
            Log($"Unity Version: {Application.unityVersion}");
            Log($"Output Path: {buildRootPath}");
            Log("");

            FlushToFile();
        }

        public static void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logBuffer.AppendLine(timestampedMessage);
            Debug.Log($"[KiqqiBuild] {message}");
        }

        public static void LogWarning(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] WARNING: {message}";
            logBuffer.AppendLine(timestampedMessage);
            Debug.LogWarning($"[KiqqiBuild] {message}");
        }

        public static void LogError(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}";
            logBuffer.AppendLine(timestampedMessage);
            Debug.LogError($"[KiqqiBuild] {message}");
        }

        public static void LogSeparator()
        {
            logBuffer.AppendLine("─────────────────────────────────────────────────");
        }

        public static void FlushToFile()
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    string directory = Path.GetDirectoryName(logFilePath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(logFilePath, logBuffer.ToString());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[KiqqiBuild] Failed to write log file: {e.Message}");
                }
            }
        }

        public static void FinalizeLog(int successCount, int failCount, int totalCount, TimeSpan duration)
        {
            LogSeparator();
            Log($"Build Summary:");
            Log($"  Total Games: {totalCount}");
            Log($"  Successful: {successCount}");
            Log($"  Failed: {failCount}");
            Log($"  Success Rate: {(totalCount > 0 ? (successCount * 100 / totalCount) : 0)}%");
            Log($"  Duration: {duration.Hours}h {duration.Minutes}m {duration.Seconds}s");
            Log($"Completed: {DateTime.Now}");
            Log($"=== END OF LOG ===");

            FlushToFile();

            Debug.Log($"[KiqqiBuild] Full log saved to: {logFilePath}");
        }
    }
}
