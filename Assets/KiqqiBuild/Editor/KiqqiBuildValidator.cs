using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Kiqqi.Framework;

namespace Kiqqi.BuildSystem.Editor
{
    public enum SceneBuildType
    {
        Mobile,
        Desktop,
        Unknown
    }

    public enum ValidationStatus
    {
        Valid,
        Warning,
        Error
    }

    [System.Serializable]
    public struct LocalizationFileResult
    {
        public string FileName;
        public bool IsValid;
        public bool IsWarning;
        public string Message;
    }

    public class ValidationResult
    {
        public ValidationStatus Status;
        public string Message;
        public string Suggestion;

        public static ValidationResult Success(string message = "")
        {
            return new ValidationResult { Status = ValidationStatus.Valid, Message = message ?? "Valid" };
        }

        public static ValidationResult Valid(string message = "")
        {
            return Success(message);
        }

        public static ValidationResult Warning(string message, string suggestion = "")
        {
            return new ValidationResult { Status = ValidationStatus.Warning, Message = message, Suggestion = suggestion };
        }

        public static ValidationResult Error(string message, string suggestion = "")
        {
            return new ValidationResult { Status = ValidationStatus.Error, Message = message, Suggestion = suggestion };
        }
    }

    public class GameValidationReport
    {
        public string GameId;
        public string DisplayName;
        public ValidationResult MobileResult;
        public ValidationResult DesktopResult;
        public ValidationResult SceneCheckResult;
        public ValidationResult UITitleCheckResult;
        public List<LocalizationFileResult> LocalizationResults;
        
        public bool IsValid => (MobileResult == null || MobileResult.Status != ValidationStatus.Error) &&
                               (DesktopResult == null || DesktopResult.Status != ValidationStatus.Error) &&
                               (SceneCheckResult == null || SceneCheckResult.Status != ValidationStatus.Error) &&
                               (UITitleCheckResult == null || UITitleCheckResult.Status != ValidationStatus.Error) &&
                               (LocalizationResults == null || LocalizationResults.All(r => r.IsValid));
        public bool HasErrors => !IsValid;
        public bool HasWarnings => (MobileResult != null && MobileResult.Status == ValidationStatus.Warning) ||
                                   (DesktopResult != null && DesktopResult.Status == ValidationStatus.Warning) ||
                                   (SceneCheckResult != null && SceneCheckResult.Status == ValidationStatus.Warning) ||
                                   (UITitleCheckResult != null && UITitleCheckResult.Status == ValidationStatus.Warning) ||
                                   (LocalizationResults != null && LocalizationResults.Any(r => r.IsWarning));
    }

    public static class KiqqiBuildValidator
    {
        public static SceneBuildType DetectSceneType(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return SceneBuildType.Unknown;

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName.EndsWith("_m"))
                return SceneBuildType.Mobile;

            if (sceneName.EndsWith("_w") || sceneName.EndsWith("_web"))
                return SceneBuildType.Desktop;

            return SceneBuildType.Unknown;
        }

        public static ValidationResult ValidateProfile(BuildProfile profile, SceneBuildType expectedType, string gameId)
        {
            if (profile == null)
                return ValidationResult.Error("Build Profile not assigned", "Assign a Build Profile asset");

            var scenes = profile.scenes;
            if (scenes == null || scenes.Length == 0)
                return ValidationResult.Error("No scene assigned to Build Profile", "Add a scene to the Build Profile");

            if (scenes.Length > 1)
                return ValidationResult.Error($"Build Profile has {scenes.Length} scenes (expected 1)", "Remove extra scenes from Build Profile");

            string scenePath = scenes[0].path;
            if (string.IsNullOrEmpty(scenePath))
                return ValidationResult.Error("Scene path is empty in Build Profile");

            SceneBuildType detectedType = DetectSceneType(scenePath);

            if (detectedType == SceneBuildType.Unknown)
            {
                return ValidationResult.Warning(
                    $"Scene '{Path.GetFileName(scenePath)}' doesn't follow naming convention",
                    "Scene should end with _m (mobile) or _w/_web (desktop)");
            }

            if (detectedType != expectedType)
            {
                string expected = expectedType == SceneBuildType.Mobile ? "_m" : "_w or _web";
                string detected = detectedType == SceneBuildType.Mobile ? "_m" : "_w/_web";

                return ValidationResult.Error(
                    $"Scene ends with {detected} but expected {expected} for {expectedType} build",
                    "Assign the correct Build Profile or scene");
            }

            if (detectedType == SceneBuildType.Desktop && scenePath.EndsWith("_web.unity"))
            {
                return ValidationResult.Warning(
                    "Scene uses _web suffix (non-standard, expected _w)",
                    "Consider renaming to _w for consistency");
            }

            if (!File.Exists(scenePath))
                return ValidationResult.Error($"Scene file not found: {scenePath}", "Check scene path or regenerate Build Profile");

            return ValidationResult.Success($"Valid {expectedType} scene: {Path.GetFileName(scenePath)}");
        }

        public static GameValidationReport ValidateGameConfig(GameBuildConfig config, bool validateMobile, bool validateDesktop)
        {
            var report = new GameValidationReport
            {
                GameId = config?.gameId ?? "Unknown",
                DisplayName = config?.displayName ?? "Unknown"
            };

            if (config == null)
            {
                report.MobileResult = ValidationResult.Error("Config is null");
                return report;
            }

            if (string.IsNullOrEmpty(config.gameId))
            {
                report.MobileResult = ValidationResult.Error("gameId is empty", "Set a unique game identifier");
                return report;
            }

            if (validateMobile && config.HasMobileProfile)
            {
                report.MobileResult = ValidateProfile(config.mobileBuildProfile, SceneBuildType.Mobile, config.gameId);
                
                if (report.MobileResult.Status == ValidationStatus.Valid)
                {
                    report.SceneCheckResult = ValidateAndFixSceneGameIds(config.mobileBuildProfile, config.gameId);
                }
            }

            if (validateDesktop && config.HasDesktopProfile)
            {
                report.DesktopResult = ValidateProfile(config.desktopBuildProfile, SceneBuildType.Desktop, config.gameId);
                
                if (validateMobile && config.HasMobileProfile)
                {
                }
                else if (report.DesktopResult.Status == ValidationStatus.Valid)
                {
                    report.SceneCheckResult = ValidateAndFixSceneGameIds(config.desktopBuildProfile, config.gameId);
                }
            }

            if (validateMobile && !config.HasMobileProfile)
            {
                report.MobileResult = ValidationResult.Warning("No mobile Build Profile assigned", "Mobile builds will be skipped");
            }

            if (validateDesktop && !config.HasDesktopProfile)
            {
                report.DesktopResult = ValidationResult.Warning("No desktop Build Profile assigned", "Desktop builds will be skipped");
            }

            // UI Title validation - use first available profile
            BuildProfile profileForUICheck = config.mobileBuildProfile ?? config.desktopBuildProfile;
            if (profileForUICheck != null)
            {
                report.UITitleCheckResult = ValidateSceneTitleTextPanels(profileForUICheck, config.displayName);
            }

            // Localization file existence check
            report.LocalizationResults = ValidateLocalizationFiles(config.displayName);

            return report;
        }

        public static GameValidationReport ValidateGame(KiqqiBuildSettings settings, bool validateMobile, bool validateDesktop)
        {
            if (settings == null || settings.gameConfig == null)
            {
                return new GameValidationReport
                {
                    GameId = "Unknown",
                    DisplayName = "Unknown",
                    MobileResult = ValidationResult.Error("Settings not configured"),
                    DesktopResult = ValidationResult.Error("Settings not configured")
                };
            }

            return ValidateGameConfig(settings.gameConfig, validateMobile, validateDesktop);
        }

        public static ValidationResult ValidateAndFixSceneGameIds(BuildProfile profile, string expectedGameId)
        {
            if (profile == null || profile.scenes == null || profile.scenes.Length == 0)
                return ValidationResult.Valid("No scene to validate");

            string scenePath = profile.scenes[0].path;
            
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
                return ValidationResult.Error($"Scene file not found: {scenePath}");

            Scene currentScene = EditorSceneManager.GetActiveScene();
            bool needToReopen = currentScene.path != scenePath;
            Scene targetScene = default;

            try
            {
                if (needToReopen)
                {
                    if (currentScene.isDirty)
                    {
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            return ValidationResult.Warning("Scene validation cancelled", "Save current scene to continue");
                    }
                    
                    targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    targetScene = currentScene;
                }

                bool sceneModified = false;
                int fixedCount = 0;
                string errors = "";

                KiqqiDataManager dataManager = Object.FindFirstObjectByType<KiqqiDataManager>();
                if (dataManager != null)
                {
                    if (dataManager.gameDefinition == null)
                    {
                        errors += "• KiqqiDataManager.gameDefinition is null\n";
                    }
                    else
                    {
                        string definitionGameId = dataManager.gameDefinition.gameId;
                        string normalizedExpected = expectedGameId.ToLower().Replace(" ", "").Replace("-", "");
                        string normalizedActual = definitionGameId.ToLower().Replace(" ", "").Replace("-", "");

                        if (normalizedActual != normalizedExpected)
                        {
                            string fixedId = FixGameIdCasing(expectedGameId);
                            dataManager.gameDefinition.gameId = fixedId;
                            EditorUtility.SetDirty(dataManager.gameDefinition);
                            sceneModified = true;
                            fixedCount++;
                            Debug.Log($"[Validator] Fixed KiqqiGameDefinition.gameId: '{definitionGameId}' → '{fixedId}'");
                        }
                    }
                }
                else
                {
                    errors += "• KiqqiDataManager not found in scene\n";
                }

                KiqqiScoringApi scoringApi = Object.FindFirstObjectByType<KiqqiScoringApi>();
                if (scoringApi != null)
                {
                    string normalizedExpected = expectedGameId.ToLower().Replace(" ", "").Replace("-", "");
                    string normalizedActual = scoringApi.gameId.ToLower().Replace(" ", "").Replace("-", "");

                    if (normalizedActual != normalizedExpected)
                    {
                        string fixedId = FixGameIdCasing(expectedGameId);
                        scoringApi.gameId = fixedId;
                        EditorUtility.SetDirty(scoringApi);
                        sceneModified = true;
                        fixedCount++;
                        Debug.Log($"[Validator] Fixed KiqqiScoringApi.gameId: '{scoringApi.gameId}' → '{fixedId}'");
                    }
                }
                else
                {
                    errors += "• KiqqiScoringApi not found in scene\n";
                }

                KiqqiMiniGameManagerBase gameManager = Object.FindFirstObjectByType<KiqqiMiniGameManagerBase>();
                if (gameManager != null)
                {
                    string normalizedExpected = expectedGameId.ToLower().Replace(" ", "").Replace("-", "");
                    string normalizedActual = gameManager.gameID.ToLower().Replace(" ", "").Replace("-", "");

                    if (normalizedActual != normalizedExpected)
                    {
                        string fixedId = FixGameIdCasing(expectedGameId);
                        gameManager.gameID = fixedId;
                        EditorUtility.SetDirty(gameManager);
                        sceneModified = true;
                        fixedCount++;
                        Debug.Log($"[Validator] Fixed {gameManager.GetType().Name}.gameID: '{gameManager.gameID}' → '{fixedId}'");
                    }
                }
                else
                {
                    errors += "• KiqqiMiniGameManagerBase not found in scene\n";
                }

                if (sceneModified)
                {
                    EditorSceneManager.SaveScene(targetScene);
                    AssetDatabase.SaveAssets();
                }

                if (!string.IsNullOrEmpty(errors))
                {
                    return ValidationResult.Error($"Scene validation failed:\n{errors.TrimEnd()}");
                }

                if (fixedCount > 0)
                {
                    return ValidationResult.Valid($"Scene validated, {fixedCount} gameId(s) auto-fixed");
                }

                return ValidationResult.Valid("Scene validated, all gameIds match");
            }
            catch (System.Exception ex)
            {
                return ValidationResult.Error($"Scene validation exception: {ex.Message}");
            }
            finally
            {
                if (needToReopen && currentScene.IsValid() && !string.IsNullOrEmpty(currentScene.path))
                {
                    EditorSceneManager.OpenScene(currentScene.path, OpenSceneMode.Single);
                }
            }
        }

        private static string FixGameIdCasing(string gameId)
        {
            return gameId.ToLower().Replace(" ", "").Replace("_", "");
        }

        // ================================================== LOCALIZATION VALIDATION

        /// <summary>
        /// Validates UI title text setup and localization file presence.
        /// Checks for MainView and ResultsEndView panels with correct TitleText configuration.
        /// </summary>
        public static List<LocalizationFileResult> ValidateLocalizationFiles(string displayName)
        {
            var results = new List<LocalizationFileResult>();
            
            // 1. Check language files existence and freshness
            results.AddRange(CheckLanguageFilesExistence());
            
            return results;
        }

        private static List<LocalizationFileResult> CheckLanguageFilesExistence()
        {
            var results = new List<LocalizationFileResult>();
            string resourcesPath = "Assets/Resources";
            string[] requiredLanguages = { "en.json", "de.json", "fr.json", "it.json" };

            foreach (string langFile in requiredLanguages)
            {
                string filePath = Path.Combine(resourcesPath, langFile);
                
                if (!File.Exists(filePath))
                {
                    results.Add(new LocalizationFileResult
                    {
                        FileName = langFile,
                        IsValid = false,
                        IsWarning = false,
                        Message = "File not found"
                    });
                    continue;
                }

                // Check generatedAt timestamp
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    string generatedAt = ExtractGeneratedAt(jsonContent);
                    
                    if (!string.IsNullOrEmpty(generatedAt))
                    {
                        if (System.DateTime.TryParse(generatedAt, out System.DateTime generated))
                        {
                            System.TimeSpan age = System.DateTime.UtcNow - generated;
                            
                            if (age.TotalDays > 7)
                            {
                                results.Add(new LocalizationFileResult
                                {
                                    FileName = langFile,
                                    IsValid = true,
                                    IsWarning = true,
                                    Message = $"OK - File is {(int)age.TotalDays} days old (generated: {generated:yyyy-MM-dd})"
                                });
                            }
                            else
                            {
                                results.Add(new LocalizationFileResult
                                {
                                    FileName = langFile,
                                    IsValid = true,
                                    IsWarning = false,
                                    Message = $"OK - File is {(int)age.TotalDays} days old"
                                });
                            }
                        }
                        else
                        {
                            results.Add(new LocalizationFileResult
                            {
                                FileName = langFile,
                                IsValid = true,
                                IsWarning = false,
                                Message = "OK - Found (unable to parse date)"
                            });
                        }
                    }
                    else
                    {
                        results.Add(new LocalizationFileResult
                        {
                            FileName = langFile,
                            IsValid = true,
                            IsWarning = false,
                            Message = "OK - Found"
                        });
                    }
                }
                catch (System.Exception ex)
                {
                    results.Add(new LocalizationFileResult
                    {
                        FileName = langFile,
                        IsValid = false,
                        IsWarning = false,
                        Message = $"Error reading file: {ex.Message}"
                    });
                }
            }

            return results;
        }

        private static string ExtractGeneratedAt(string jsonContent)
        {
            // Extract "generatedAt": "2025-12-02T13:57:52Z" from metadata
            var lines = jsonContent.Split('\n');
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("\"generatedAt\":"))
                {
                    return ExtractJsonValue(trimmed);
                }
            }
            return null;
        }

        /// <summary>
        /// Validates UI title text panels in the loaded scene.
        /// Checks for MainView and ResultsEndView panels with proper TitleText setup.
        /// </summary>
        public static ValidationResult ValidateSceneTitleTextPanels(BuildProfile profile, string displayName)
        {
            if (profile == null || profile.scenes == null || profile.scenes.Length == 0)
                return ValidationResult.Error("No scene in Build Profile");

            string scenePath = profile.scenes[0].path;
            var currentScene = EditorSceneManager.GetActiveScene();
            bool needsRestore = currentScene.path != scenePath;

            try
            {
                // Open the scene if needed
                if (needsRestore)
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }

                var scene = EditorSceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                // Find Canvas
                GameObject canvas = null;
                foreach (var root in rootObjects)
                {
                    if (root.name.ToLower().Contains("canvas"))
                    {
                        canvas = root;
                        break;
                    }
                }

                if (canvas == null)
                    return ValidationResult.Error("Canvas not found in scene");

                // Check MainView and ResultsEndView
                var issues = new List<string>();
                
                CheckTitleTextPanel(canvas, "MainView", displayName, issues);
                CheckTitleTextPanel(canvas, "ResultsEndView", displayName, issues);

                if (issues.Count > 0)
                {
                    return ValidationResult.Error($"UI Title Issues: {string.Join("; ", issues)}");
                }

                return ValidationResult.Valid("UI title panels validated");
            }
            catch (System.Exception ex)
            {
                return ValidationResult.Error($"Error validating UI panels: {ex.Message}");
            }
            finally
            {
                if (needsRestore)
                {
                    EditorSceneManager.OpenScene(currentScene.path, OpenSceneMode.Single);
                }
            }
        }

        private static void CheckTitleTextPanel(GameObject canvas, string panelSuffix, string displayName, List<string> issues)
        {
            // Find panel ending with panelSuffix (e.g., "pfMainView", "caMainView")
            GameObject panel = null;
            foreach (Transform child in canvas.transform)
            {
                if (child.name.EndsWith(panelSuffix))
                {
                    panel = child.gameObject;
                    break;
                }
            }

            if (panel == null)
            {
                issues.Add($"Panel ending with '{panelSuffix}' not found");
                return;
            }

            // Find direct child with "TitleText" in name
            GameObject titleTextObj = null;
            foreach (Transform child in panel.transform)
            {
                if (child.name.Contains("TitleText"))
                {
                    titleTextObj = child.gameObject;
                    break;
                }
            }

            if (titleTextObj == null)
            {
                issues.Add($"{panel.name}: No TitleText child found");
                return;
            }

            // Check Text component
            var textComponent = titleTextObj.GetComponent<UnityEngine.UI.Text>();
            if (textComponent == null)
            {
                issues.Add($"{panel.name}/{titleTextObj.name}: No Text component");
                return;
            }

            string expectedText = displayName.ToUpper();
            if (!textComponent.text.Contains(expectedText))
            {
                issues.Add($"{panel.name}/{titleTextObj.name}: Text is '{textComponent.text}' (expected '{expectedText}')");
            }

            // Check that KiqqiLocalizedText component does NOT exist
            var localizedType = System.Type.GetType("Kiqqi.Framework.KiqqiLocalizedText, Assembly-CSharp") 
                             ?? System.Type.GetType("KiqqiLocalizedText, Assembly-CSharp");
            
            if (localizedType != null)
            {
                var localizedComponent = titleTextObj.GetComponent(localizedType);
                if (localizedComponent != null)
                {
                    issues.Add($"{panel.name}/{titleTextObj.name}: Has KiqqiLocalizedText component (should not be localized)");
                }
            }
        }

        private static string ExtractJsonValue(string line)
        {
            // Extract value from: "key": "value",
            int firstQuote = line.IndexOf('"', line.IndexOf(':') + 1);
            int lastQuote = line.LastIndexOf('"');
            
            if (firstQuote >= 0 && lastQuote > firstQuote)
            {
                return line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            }

            return string.Empty;
        }
    }
}
