using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Central scoring API for all Kiqqi games.
    /// Handles URL parsing, API root setup, and posting score data.
    /// Works in WebGL and Editor. Keeps structure for server-based tracking.
    /// </summary>
    public class KiqqiScoringApi : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, will send an additional 'correct_answers' field when posting.")]
        public bool useCorrectCount = false;

        [Tooltip("Default game ID (overridden by URL if present).")]
        [HideInInspector]
        public string gameId = "kiqqi-default";

        [Tooltip("Default API root (overridden by URL if present).")]
        [HideInInspector]
        public string apiRoot = "https://brain-teacher-api.flowly.com";

        [Header("Runtime (debug)")]
        [SerializeField] private string scoreUrl;
        [SerializeField] private string currentUrl;
        [SerializeField] private Dictionary<string, string> queryParts = new();

        private bool initialized;

        // ----------------------------------------------------------
        public void Initialize(KiqqiDataManager data)
        {
            if (initialized) return;
            initialized = true;

            // ----------------------------------------------------------
            // 1. Auto-apply the global game definition from DataManager
            // ----------------------------------------------------------
            var def = data != null ? data.gameDefinition : null;
            if (def != null)
            {
                gameId = def.gameId;
                apiRoot = def.apiRoot;
                Debug.Log($"[KiqqiScoringApi] GameDefinition applied > GameID={gameId}, Root={apiRoot}");
            }

            // ----------------------------------------------------------
            // 2. Continue existing setup logic
            // ----------------------------------------------------------
            currentUrl = Application.absoluteURL;
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(currentUrl))
                currentUrl = "EDITOR/localhost";
#endif

            ParseUrl(currentUrl);
            SetupData();

            scoreUrl = $"{apiRoot.TrimEnd('/')}/statistics/";
            Debug.Log($"[KiqqiScoringApi] Initialized > Root={apiRoot}, GameID={gameId}");
        }


        // ----------------------------------------------------------
        private void ParseUrl(string url)
        {
            queryParts.Clear();
            if (string.IsNullOrEmpty(url)) return;

            if (Uri.TryCreate(url, UriKind.Absolute, out var appUrl))
            {
                string[] parts = appUrl.Query.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string p in parts)
                {
                    string[] keys = p.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (keys.Length >= 2)
                    {
                        string key = keys[0];
                        string val = UnityWebRequest.UnEscapeURL(keys[1]);
                        queryParts[key] = val;
                    }
                }
            }
        }

        private void SetupData()
        {
            if (queryParts.TryGetValue("api_root", out string root))
                apiRoot = root;
            if (queryParts.TryGetValue("game_id", out string gid))
                gameId = gid;
        }

        // ----------------------------------------------------------
        /// <summary>
        /// Posts score asynchronously. Optionally includes correctAnswers if enabled.
        /// </summary>
        public async void PostScore(int score, int correctAnswers = 0)
        {
            if (string.IsNullOrEmpty(scoreUrl))
                scoreUrl = $"{apiRoot.TrimEnd('/')}/statistics/";

            string target = scoreUrl + gameId;

            WWWForm form = new WWWForm();
            form.AddField("score", score);
            if (useCorrectCount)
                form.AddField("correct_answers", correctAnswers);

            using UnityWebRequest www = UnityWebRequest.Post(target, form);
            www.timeout = 10;

            // Log the request details before sending
            Debug.Log($"[KiqqiScoringApi] POST INIT\n" +
                      $"  URL: {target}\n" +
                      $"  Game ID: {gameId}\n" +
                      $"  API Root: {apiRoot}\n" +
                      $"  Fields:\n" +
                      $"    - score = {score}\n" +
                      $"    - correct_answers = {(useCorrectCount ? correctAnswers.ToString() : "(disabled)")}\n" +
                      $"------------------------------------------");

            try
            {
                var op = www.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                string responseText = www.downloadHandler != null ? www.downloadHandler.text : "(no response body)";
                long code = www.responseCode;

                if (www.result != UnityWebRequest.Result.Success || code >= 400)
                {
                    Debug.Log($"[KiqqiScoringApi] <color=red>ERROR</color> RESPONSE\n" +
                                     $"  HTTP {(int)code}\n" +
                                     $"  Result: {www.result}\n" +
                                     $"  Error: {www.error}\n" +
                                     $"  URL: {target}\n" +
                                     $"  Request Body: score={score}, correct={correctAnswers}\n" +
                                     $"  Response:\n{responseText}\n" +
                                     $"------------------------------------------");
                }
                else
                {
                    Debug.Log($"[KiqqiScoringApi] SUCCESS\n" +
                              $"  HTTP {(int)code}\n" +
                              $"  Response:\n{responseText}\n" +
                              $"------------------------------------------");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[KiqqiScoringApi] EXCEPTION\n" +
                               $"  {e.GetType().Name}: {e.Message}\n" +
                               $"{e.StackTrace}\n" +
                               $"------------------------------------------");
            }
        }


        // ----------------------------------------------------------
        /// <summary>
        /// Quick manual test in Editor.
        /// </summary>
        [ContextMenu("Test Post")]
        private void TestPost()
        {
            int randomScore = UnityEngine.Random.Range(10, 100);
            PostScore(randomScore);
        }

        [ContextMenu("Test URL Parse")]
        private void TestUrl()
        {
            ParseUrl("https://example.com/?api_root=https://brain-teacher-api.flowly.com&game_id=test123");
            SetupData();
            Debug.Log($"[KiqqiScoringApi] Test Parse > Root={apiRoot}, GameID={gameId}");
        }
    }
}
