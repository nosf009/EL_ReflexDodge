using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using System;
using System.IO;

namespace Kiqqi.BuildSystem.Editor
{
    public class KiqqiBuildWindow_New : EditorWindow
    {
        private enum Tab
        {
            Setup,
            Game,
            Build,
            History
        }

        private Tab currentTab = Tab.Setup;
        private KiqqiBuildSettings settings;
        private Vector2 scrollPosition;

        private bool buildMobile = true;
        private bool buildDesktop = true;

        private GameValidationReport validationReport;
        private bool hasValidated = false;
        private bool isBuilding = false;
        private float buildProgress = 0f;
        private string currentBuildStatus = "";

        [MenuItem("Kiqqi/Build System")]
        public static void ShowWindow()
        {
            var window = GetWindow<KiqqiBuildWindow_New>("Kiqqi Build System");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            LoadOrCreateSettings();
        }

        private void LoadOrCreateSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:KiqqiBuildSettings");
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<KiqqiBuildSettings>(path);
            }
            else
            {
                settings = CreateInstance<KiqqiBuildSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/KiqqiBuild/KiqqiBuildSettings.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("Created new KiqqiBuildSettings asset");
            }

            if (settings.gameConfig == null)
                settings.gameConfig = new GameBuildConfig();

            if (string.IsNullOrEmpty(settings.projectRootPath))
            {
                settings.projectRootPath = Path.GetDirectoryName(Application.dataPath);
                EditorUtility.SetDirty(settings);
            }
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                LoadOrCreateSettings();
                if (settings == null)
                {
                    EditorGUILayout.HelpBox("Failed to load settings", MessageType.Error);
                    return;
                }
            }

            DrawTabs();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case Tab.Setup:
                    DrawSetupTab();
                    break;
                case Tab.Game:
                    DrawGameTab();
                    break;
                case Tab.Build:
                    DrawBuildTab();
                    break;
                case Tab.History:
                    DrawHistoryTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Setup, "Setup", EditorStyles.toolbarButton))
                currentTab = Tab.Setup;
            
            if (GUILayout.Toggle(currentTab == Tab.Game, "Game", EditorStyles.toolbarButton))
                currentTab = Tab.Game;
            
            if (GUILayout.Toggle(currentTab == Tab.Build, "Build", EditorStyles.toolbarButton))
                currentTab = Tab.Build;
            
            if (GUILayout.Toggle(currentTab == Tab.History, "History", EditorStyles.toolbarButton))
                currentTab = Tab.History;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetupTab()
        {
            EditorGUILayout.LabelField("Build Paths", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            settings.buildRootPath = EditorGUILayout.TextField("Build Root Path", settings.buildRootPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Build Root Folder", settings.buildRootPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    settings.buildRootPath = path;
                    EditorUtility.SetDirty(settings);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Project Root", settings.projectRootPath);
            settings.artSourceFolderName = EditorGUILayout.TextField("Art Folder Name", settings.artSourceFolderName);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Options", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            settings.verifyBuilds = EditorGUILayout.Toggle("Verify Builds", settings.verifyBuilds);
            settings.copyArtFolders = EditorGUILayout.Toggle("Copy Art Folders", settings.copyArtFolders);
            settings.createZipArchives = EditorGUILayout.Toggle("Create ZIP Archives", settings.createZipArchives);
            settings.cleanTempOnComplete = EditorGUILayout.Toggle("Clean Temp on Complete", settings.cleanTempOnComplete);
            settings.buildTimeoutSeconds = EditorGUILayout.IntField("Build Timeout (seconds)", settings.buildTimeoutSeconds);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            if (GUILayout.Button("Save Settings"))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("Settings saved");
            }
        }

        private void DrawGameTab()
        {
            EditorGUILayout.LabelField("Game Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (settings.gameConfig == null)
                settings.gameConfig = new GameBuildConfig();

            var config = settings.gameConfig;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            config.gameId = EditorGUILayout.TextField("Game ID", config.gameId);
            config.displayName = EditorGUILayout.TextField("Display Name", config.displayName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Profiles", EditorStyles.boldLabel);
            config.mobileBuildProfile = EditorGUILayout.ObjectField("Mobile Profile (_m)", config.mobileBuildProfile, typeof(BuildProfile), false) as BuildProfile;
            config.desktopBuildProfile = EditorGUILayout.ObjectField("Desktop Profile (_w)", config.desktopBuildProfile, typeof(BuildProfile), false) as BuildProfile;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            config.artFolderName = EditorGUILayout.TextField("Art Folder Name", config.artFolderName);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Configuration", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("Game configuration saved");
            }
        }

        private void DrawValidationReport(GameValidationReport report)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"Validation: {report.DisplayName}", EditorStyles.boldLabel);
            
            if (report.MobileResult != null)
            {
                DrawValidationResult("Mobile", report.MobileResult);
            }
            
            if (report.DesktopResult != null)
            {
                DrawValidationResult("Desktop", report.DesktopResult);
            }

            if (report.SceneCheckResult != null)
            {
                DrawValidationResult("Scene Check", report.SceneCheckResult);
            }

            if (report.UITitleCheckResult != null)
            {
                DrawValidationResult("UI Title Check", report.UITitleCheckResult);
            }

            // Localization results
            if (report.LocalizationResults != null && report.LocalizationResults.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Localization Files:", EditorStyles.boldLabel);
                
                foreach (var locResult in report.LocalizationResults)
                {
                    DrawLocalizationResult(locResult);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawValidationResult(string label, ValidationResult result)
        {
            EditorGUILayout.BeginHorizontal();
            
            string icon = result.Status == ValidationStatus.Valid ? "✓" : (result.Status == ValidationStatus.Warning ? "⚠" : "✗");
            Color color = result.Status == ValidationStatus.Valid ? Color.green : (result.Status == ValidationStatus.Warning ? Color.yellow : Color.red);
            
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"{icon} {label}", GUILayout.Width(100));
            GUI.color = oldColor;
            
            EditorGUILayout.LabelField(result.Message);
            
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(result.Suggestion))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"→ {result.Suggestion}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawLocalizationResult(LocalizationFileResult result)
        {
            EditorGUILayout.BeginHorizontal();
            
            string icon = result.IsValid ? (result.IsWarning ? "⚠" : "✓") : "✗";
            Color color = result.IsValid ? (result.IsWarning ? Color.yellow : Color.green) : Color.red;
            
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"  {icon} {result.FileName}", GUILayout.Width(120));
            GUI.color = oldColor;
            
            EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBuildTab()
        {
            EditorGUILayout.LabelField("Build Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Game: {settings.gameConfig.displayName} ({settings.gameConfig.gameId})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Mobile: {(settings.gameConfig.HasMobileProfile ? "✓" : "✗")} | Desktop: {(settings.gameConfig.HasDesktopProfile ? "✓" : "✗")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Build Types:", EditorStyles.boldLabel);
            buildMobile = EditorGUILayout.Toggle("Build Mobile (_m)", buildMobile);
            buildDesktop = EditorGUILayout.Toggle("Build Desktop (_w)", buildDesktop);

            if (hasValidated && validationReport != null)
            {
                EditorGUILayout.Space();
                DrawValidationReport(validationReport);
            }

            EditorGUILayout.Space();

            if (isBuilding)
            {
                EditorGUILayout.LabelField(currentBuildStatus, EditorStyles.boldLabel);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), buildProgress, $"{(buildProgress * 100):F0}%");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Validate", GUILayout.Height(30)))
                {
                    RunValidation();
                }

                GUI.enabled = hasValidated;
                if (GUILayout.Button("START BUILD", GUILayout.Height(30)))
                {
                    StartBuild();
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }
        }

        private void RunValidation()
        {
            validationReport = KiqqiBuildValidator.ValidateGame(settings, buildMobile, buildDesktop);
            hasValidated = true;
        }

        private void StartBuild()
        {
            if (!hasValidated)
            {
                EditorUtility.DisplayDialog("Validation Required", "Please run validation before building", "OK");
                return;
            }

            if (validationReport.HasErrors)
            {
                EditorUtility.DisplayDialog(
                    "Validation Failed", 
                    "Build cannot proceed due to validation errors.\n\nPlease fix all errors and validate again before building.", 
                    "OK");
                return;
            }

            isBuilding = true;
            buildProgress = 0f;

            KiqqiBuildLogger.StartNewLog(settings.buildRootPath);
            KiqqiBuildLogger.Log($"=== Kiqqi Build Started ===");
            KiqqiBuildLogger.Log($"Game: {settings.gameConfig.displayName}");
            KiqqiBuildLogger.Log($"Mobile: {buildMobile} | Desktop: {buildDesktop}");

            DateTime startTime = DateTime.Now;
            int successCount = 0;
            int failCount = 0;

            var config = settings.gameConfig;

            if (buildMobile && config.HasMobileProfile)
            {
                currentBuildStatus = $"Building {config.displayName} (Mobile)...";
                var result = KiqqiBuildPipeline.BuildGame(config, BuildType.Mobile, settings.buildRootPath, settings.TempBuildPath, settings);
                
                if (result.Success)
                {
                    successCount++;
                    KiqqiBuildLogger.Log($"✓ Mobile build completed in {result.Duration.TotalSeconds:F1}s");
                }
                else
                {
                    failCount++;
                    KiqqiBuildLogger.LogError($"✗ Mobile build failed: {result.ErrorMessage}");
                }
            }

            if (buildDesktop && config.HasDesktopProfile)
            {
                currentBuildStatus = $"Building {config.displayName} (Desktop)...";
                var result = KiqqiBuildPipeline.BuildGame(config, BuildType.Desktop, settings.buildRootPath, settings.TempBuildPath, settings);
                
                if (result.Success)
                {
                    successCount++;
                    KiqqiBuildLogger.Log($"✓ Desktop build completed in {result.Duration.TotalSeconds:F1}s");
                }
                else
                {
                    failCount++;
                    KiqqiBuildLogger.LogError($"✗ Desktop build failed: {result.ErrorMessage}");
                }
            }

            TimeSpan totalDuration = DateTime.Now - startTime;
            int totalBuilds = successCount + failCount;

            KiqqiBuildLogger.FinalizeLog(totalBuilds, successCount, failCount, totalDuration);

            isBuilding = false;
            currentBuildStatus = "Build Complete";

            if (failCount == 0)
            {
                EditorUtility.DisplayDialog("Build Complete", $"All builds succeeded!\n\nDuration: {totalDuration.TotalMinutes:F1} minutes", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Build Complete", $"Builds completed with {failCount} failure(s)\n\nCheck the log for details", "OK");
            }
        }

        private void DrawHistoryTab()
        {
            EditorGUILayout.LabelField("Build History", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Build logs are saved in the build root path", MessageType.Info);

            if (GUILayout.Button("Open Build Root Folder"))
            {
                if (Directory.Exists(settings.buildRootPath))
                {
                    System.Diagnostics.Process.Start(settings.buildRootPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Folder Not Found", "Build root path does not exist", "OK");
                }
            }
        }
    }
}
